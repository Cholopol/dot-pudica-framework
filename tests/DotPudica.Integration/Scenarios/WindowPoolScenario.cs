using DotPudica.Godot.Views;
using DotPudica.Integration.Fixtures;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Pooled window golden path: Dismiss recycles the node (alive, out of tree, Owned ViewModel
/// disposed); re-Show reuses the same node with a fresh ViewModel; pool overflow destroys.
/// </summary>
public sealed class WindowPoolScenario : IIntegrationScenario
{
    public string Name => "WindowPool_RecycleAndReuse";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var wm = new GodotWindowManager { Name = "TestWindowManager_Pool" };
        host.AddChild(wm);

        AppContext? app = null;
        try
        {
            try
            {
                _ = AppContext.Current;
            }
            catch (InvalidOperationException)
            {
                app = new AppContext().Initialize(null, wm);
            }

            var manager = app?.WindowManager ?? wm;
            manager.ConfigurePool<PooledWindow>(maxSize: 2);

            // ---- First cycle: fresh pooled window ----
            var first = manager.ShowPooled<PooledWindow>();
            await IntegrationTestHelpers.WaitFrames(host, 2);
            var vm1 = first.WindowVm;
            if (vm1 is null)
                return IntegrationResult.Fail(Name, "First activation did not create a ViewModel");
            if (!first.IsWindowVisible)
                return IntegrationResult.Fail(Name, "Pooled window not visible after Show");

            vm1.Title = "one";
            await IntegrationTestHelpers.WaitFrames(host, 2);
            if (first.TitleLabel.Text != "one")
                return IntegrationResult.Fail(Name, $"Binding failed on first activation, Text={first.TitleLabel.Text}");

            // ---- Dismiss: recycle, not destroy ----
            await manager.Dismiss(first, ignoreAnimation: true).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 2);

            if (!GodotObject.IsInstanceValid(first))
                return IntegrationResult.Fail(Name, "Pooled window node was destroyed on dismiss");
            if (first.IsInsideTree())
                return IntegrationResult.Fail(Name, "Pooled window is still inside the tree after dismiss");
            if (manager.Stack.Count != 0)
                return IntegrationResult.Fail(Name, $"Manager stack not cleared, Count={manager.Stack.Count}");
            if (first.Dismissed || first.Created)
                return IntegrationResult.Fail(Name, $"Lifecycle not reset for reuse (Dismissed={first.Dismissed}, Created={first.Created})");
            if (vm1.IsDisposed is false)
                return IntegrationResult.Fail(Name, "Owned ViewModel was not disposed on recycle");
            if (first.WindowVm is not null)
                return IntegrationResult.Fail(Name, "ViewModel was created while the window was parked in the pool");

            // ---- Second cycle: same node, fresh ViewModel ----
            var reused = manager.ShowPooled<PooledWindow>();
            await IntegrationTestHelpers.WaitFrames(host, 2);
            if (!ReferenceEquals(reused, first))
            {
                first.QueueFree();
                return IntegrationResult.Fail(Name, "Pool did not reuse the recycled node");
            }

            var vm2 = reused.WindowVm;
            if (vm2 is null || ReferenceEquals(vm2, vm1))
            {
                reused.QueueFree();
                return IntegrationResult.Fail(Name, "ViewModel was not recreated per activation");
            }
            if (!reused.IsWindowVisible)
            {
                reused.QueueFree();
                return IntegrationResult.Fail(Name, "Reused window not visible after re-Show");
            }

            vm2.Title = "two";
            await IntegrationTestHelpers.WaitFrames(host, 2);
            if (reused.TitleLabel.Text != "two")
            {
                reused.QueueFree();
                return IntegrationResult.Fail(Name, $"Rebind failed on reused node, Text={reused.TitleLabel.Text}");
            }

            await manager.Dismiss(reused, ignoreAnimation: true).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 2);

            // ---- Overflow: third distinct node is destroyed (capacity 2) ----
            var a = manager.ShowPooled<PooledWindow>();
            var b = manager.ShowPooled<PooledWindow>();
            var c = manager.ShowPooled<PooledWindow>();
            await IntegrationTestHelpers.WaitFrames(host, 2);

            await manager.Dismiss(a, ignoreAnimation: true).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);
            await manager.Dismiss(b, ignoreAnimation: true).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);
            await manager.Dismiss(c, ignoreAnimation: true).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 2);

            var alive = (GodotObject.IsInstanceValid(a) ? 1 : 0)
                        + (GodotObject.IsInstanceValid(b) ? 1 : 0)
                        + (GodotObject.IsInstanceValid(c) ? 1 : 0);
            if (alive != 2)
                return IntegrationResult.Fail(Name, $"Expected 2 cached nodes alive, got {alive}");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            app?.Dispose();
            wm.QueueFree();
            await IntegrationTestHelpers.WaitProcessFrame(host);
        }
    }
}
