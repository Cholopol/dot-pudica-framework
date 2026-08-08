using DotPudica.Core.ObjectPool;
using DotPudica.Godot.ObjectPool;
using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Pooled [DotPudicaView] golden path: recycle on tree-exit (node survives, ViewModel NOT
/// disposed), re-activate on the same node with a new ViewModel; pool overflow destroys.
/// </summary>
public sealed class ViewPoolScenario : IIntegrationScenario
{
    public string Name => "ViewPool_RecycleActivate_ReusesNode";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var pool = NodePool.Create<PooledItemView>(maxSize: 2);

        // ---- First activation cycle: allocate, bind, interact ----
        var vm1 = new PooledItemViewModel { Title = "first" };
        var view = pool.Allocate();
        host.AddChild(view);
        await IntegrationTestHelpers.WaitProcessFrame(host);
        view.BindShared(vm1);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        if (view.TitleLabel.Text != "first")
            return IntegrationResult.Fail(Name, $"Initial bind failed, Text={view.TitleLabel.Text}");

        vm1.Title = "first-updated";
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (view.TitleLabel.Text != "first-updated")
            return IntegrationResult.Fail(Name, $"OneWay update failed, Text={view.TitleLabel.Text}");

        view.ClickButton.EmitSignal(BaseButton.SignalName.Pressed);
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (vm1.ClickCount != 1)
            return IntegrationResult.Fail(Name, $"Command binding failed, ClickCount={vm1.ClickCount}");

        vm1.RaisePing();
        await IntegrationTestHelpers.WaitProcessFrame(host);
        if (view.PingCount != 1)
            return IntegrationResult.Fail(Name, $"Subscribe not wired on activation, PingCount={view.PingCount}");

        // ---- Recycle: leaves the tree, must NOT destroy the node or dispose the ViewModel ----
        view.GetParent()?.RemoveChild(view);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        if (!GodotObject.IsInstanceValid(view))
            return IntegrationResult.Fail(Name, "Recycled view node was destroyed");
        if (view.IsInsideTree())
            return IntegrationResult.Fail(Name, "Recycled view is still inside the tree");
        if (vm1.IsDisposed)
            return IntegrationResult.Fail(Name, "External ViewModel was disposed on recycle");

        vm1.Title = "stale";
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (view.TitleLabel.Text != "first-updated")
            return IntegrationResult.Fail(Name, $"Binding survived recycle, Text={view.TitleLabel.Text}");

        pool.Free(view);

        // ---- Second activation cycle: same node instance, new ViewModel ----
        var vm2 = new PooledItemViewModel { Title = "second" };
        var reused = pool.Allocate();
        if (!ReferenceEquals(reused, view))
        {
            pool.Dispose();
            vm1.Dispose();
            vm2.Dispose();
            return IntegrationResult.Fail(Name, "Pool did not reuse the recycled node");
        }

        host.AddChild(reused);
        await IntegrationTestHelpers.WaitProcessFrame(host);
        reused.BindShared(vm2);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        if (reused.TitleLabel.Text != "second")
        {
            Cleanup(pool, reused, vm1, vm2);
            return IntegrationResult.Fail(Name, $"Rebind on reused node failed, Text={reused.TitleLabel.Text}");
        }

        vm2.Title = "second-updated";
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (reused.TitleLabel.Text != "second-updated")
        {
            Cleanup(pool, reused, vm1, vm2);
            return IntegrationResult.Fail(Name, $"Rebind update failed, Text={reused.TitleLabel.Text}");
        }

        reused.ClickButton.EmitSignal(BaseButton.SignalName.Pressed);
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (vm2.ClickCount != 1)
        {
            Cleanup(pool, reused, vm1, vm2);
            return IntegrationResult.Fail(Name, $"Command binding leaked across cycles, ClickCount={vm2.ClickCount}");
        }

        vm2.RaisePing();
        await IntegrationTestHelpers.WaitProcessFrame(host);
        if (reused.PingCount != 2)
        {
            Cleanup(pool, reused, vm1, vm2);
            return IntegrationResult.Fail(Name, $"Subscribe re-wiring broken, PingCount={reused.PingCount}");
        }

        if (vm1.IsDisposed)
        {
            Cleanup(pool, reused, vm1, vm2);
            return IntegrationResult.Fail(Name, "First ViewModel was disposed during reuse");
        }

        // ---- Overflow: three fresh nodes, pool capacity 2 → the third is destroyed ----
        var extraA = pool.Allocate();
        var extraB = pool.Allocate();
        var overflow = pool.Allocate();
        extraA.GetParent()?.RemoveChild(extraA);
        pool.Free(extraA);
        extraB.GetParent()?.RemoveChild(extraB);
        pool.Free(extraB);
        overflow.GetParent()?.RemoveChild(overflow);
        pool.Free(overflow);
        await IntegrationTestHelpers.WaitFrames(host, 3);

        if (GodotObject.IsInstanceValid(overflow))
        {
            Cleanup(pool, reused, vm1, vm2);
            return IntegrationResult.Fail(Name, "Pool overflow node was not destroyed");
        }

        if (!GodotObject.IsInstanceValid(extraA) || !GodotObject.IsInstanceValid(extraB))
        {
            Cleanup(pool, reused, vm1, vm2);
            return IntegrationResult.Fail(Name, "Pooled nodes were destroyed while inside pool capacity");
        }

        Cleanup(pool, reused, vm1, vm2);
        return IntegrationResult.Pass(Name);
    }

    private static void Cleanup(IObjectPool<PooledItemView> pool, PooledItemView reused, PooledItemViewModel vm1, PooledItemViewModel vm2)
    {
        reused.GetParent()?.RemoveChild(reused);
        reused.QueueFree();
        pool.Dispose();
        vm1.Dispose();
        vm2.Dispose();
    }
}
