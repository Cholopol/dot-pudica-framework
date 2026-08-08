using DotPudica.Godot.ObjectPool;
using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>Pooled auto-initialized [DotPudicaView] golden path: RecycleView re-arms _ready (RequestReady), so re-entering the tree re-runs _Ready → InitializeView with a fresh Owned ViewModel — same contract as pooled windows.</summary>
public sealed class ViewPoolAutoInitScenario : IIntegrationScenario
{
    public string Name => "ViewPoolAutoInit_ReentryRecreatesViewModel";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var pool = NodePool.Create<PooledAutoInitView>(maxSize: 2);

        // ---- First activation: auto-init creates Owned VM + bindings ----
        var view = pool.Allocate();
        host.AddChild(view);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var vm1 = view.ViewVm;
        if (vm1 is null)
        {
            pool.Dispose();
            return IntegrationResult.Fail(Name, "Auto-initialized view has no ViewModel after first tree entry");
        }

        vm1.Title = "first";
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (view.TitleLabel.Text != "first")
        {
            pool.Dispose();
            return IntegrationResult.Fail(Name, $"Initial bind failed, Text={view.TitleLabel.Text}");
        }

        // ---- Recycle: leaves the tree, node survives, Owned VM disposed ----
        view.GetParent()?.RemoveChild(view);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        if (!GodotObject.IsInstanceValid(view))
        {
            pool.Dispose();
            return IntegrationResult.Fail(Name, "Recycled auto-init view node was destroyed");
        }
        if (view.IsInsideTree())
        {
            pool.Dispose();
            return IntegrationResult.Fail(Name, "Recycled auto-init view is still inside the tree");
        }
        if (!vm1.IsDisposed)
        {
            pool.Dispose();
            return IntegrationResult.Fail(Name, "Owned ViewModel was not disposed on recycle");
        }

        pool.Free(view);

        // ---- Second activation: same node, _ready re-runs → fresh VM + rebind ----
        var reused = pool.Allocate();
        if (!ReferenceEquals(reused, view))
        {
            pool.Dispose();
            return IntegrationResult.Fail(Name, "Pool did not reuse the recycled node");
        }

        host.AddChild(reused);
        await IntegrationTestHelpers.WaitFrames(host, 2);

        var vm2 = reused.ViewVm;
        if (vm2 is null || ReferenceEquals(vm2, vm1))
        {
            reused.GetParent()?.RemoveChild(reused);
            pool.Dispose();
            return IntegrationResult.Fail(Name, "Reused node did not create a fresh ViewModel (RequestReady not effective)");
        }

        vm2.Title = "second";
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (reused.TitleLabel.Text != "second")
        {
            reused.GetParent()?.RemoveChild(reused);
            pool.Dispose();
            return IntegrationResult.Fail(Name, $"Rebind on reused node failed, Text={reused.TitleLabel.Text}");
        }

        // ---- Overflow: pool capacity 2, third node destroyed on free ----
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

        var overflowDestroyed = !GodotObject.IsInstanceValid(overflow);
        var extrasAlive = GodotObject.IsInstanceValid(extraA) && GodotObject.IsInstanceValid(extraB);

        reused.GetParent()?.RemoveChild(reused);
        reused.QueueFree();
        pool.Dispose();

        if (!overflowDestroyed)
            return IntegrationResult.Fail(Name, "Pool overflow node was not destroyed");
        if (!extrasAlive)
            return IntegrationResult.Fail(Name, "Pooled nodes were destroyed while inside pool capacity");

        return IntegrationResult.Pass(Name);
    }
}
