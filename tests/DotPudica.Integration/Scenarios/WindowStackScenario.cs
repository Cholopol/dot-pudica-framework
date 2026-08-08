using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace DotPudica.Integration.Scenarios;

/// <summary>QueuedPopup items are dequeued in Dismiss order; after a Full window closes, the previous Full is restored.</summary>
public sealed partial class WindowStackScenario : IIntegrationScenario
{
    public string Name => "WindowStack_QueuedPopupDrainOrder";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var wm = new GodotWindowManager { Name = "TestWindowManager" };
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

            var full1 = new TestWindow("full-1", WindowType.Full);
            var full2 = new TestWindow("full-2", WindowType.Full);
            manager.Show(full1);
            manager.Show(full2);
            await IntegrationTestHelpers.WaitFrames(host, 1);

            if (manager.Current is not TestWindow cur || cur.Tag != "full-2")
                return IntegrationResult.Fail(Name, $"Expected Current=full-2, actual={Describe(manager.Current)}");
            if (full1.IsWindowVisible)
                return IntegrationResult.Fail(Name, "Previous Full should be hidden after Full switch");

            await manager.Dismiss(full2).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);

            var q1 = new TestWindow("q1", WindowType.QueuedPopup);
            var q2 = new TestWindow("q2", WindowType.QueuedPopup);
            var q3 = new TestWindow("q3", WindowType.QueuedPopup);
            manager.Show(q1);
            await IntegrationTestHelpers.WaitFrames(host, 1);
            if (!q1.IsWindowVisible)
                return IntegrationResult.Fail(Name, "q1 should be visible before further QueuedPopup shows enqueue");

            manager.Show(q2); // Visible QueuedPopup on stack → enqueue
            var queuedShow = manager.Show(q3);
            await queuedShow.WaitForFinish(); // Enqueue returns an already-completed transition (no Control touch)
            if (q2.Created || q3.Created)
                return IntegrationResult.Fail(Name, "Enqueued QueuedPopups must not be Created until dequeued");
            if (manager.QueuedCount != 2)
                return IntegrationResult.Fail(Name, $"Expected QueuedCount=2, actual={manager.QueuedCount}");
            await IntegrationTestHelpers.WaitFrames(host, 1);

            if (manager.Current is not TestWindow cq || cq.Tag != "q1")
                return IntegrationResult.Fail(Name, $"Expected current QueuedPopup=q1, actual={Describe(manager.Current)}");

            var order = new List<string> { cq.Tag };

            // Manager.Dismiss has animation by default; must wait for WaitForFinish, otherwise Current is still the old window.
            await manager.Dismiss(q1).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);
            if (manager.Current is not TestWindow c2 || c2.Tag != "q2")
                return IntegrationResult.Fail(Name,
                    $"Expected Current=q2 after Dismiss q1, actual={Describe(manager.Current)}; order=[{string.Join(",", order)}]");
            order.Add(c2.Tag);

            await manager.Dismiss(c2).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);
            if (manager.Current is not TestWindow c3 || c3.Tag != "q3")
                return IntegrationResult.Fail(Name,
                    $"Expected Current=q3 after Dismiss q2, actual={Describe(manager.Current)}; order=[{string.Join(",", order)}]");
            order.Add(c3.Tag);

            if (order is not ["q1", "q2", "q3"])
                return IntegrationResult.Fail(Name, $"QueuedPopup dequeue order is abnormal: [{string.Join(",", order)}]");

            await manager.Dismiss(c3, ignoreAnimation: true).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);

            // Hide must not break FIFO: a hidden QueuedPopup still occupying the top enqueues the next.
            var h1 = new TestWindow("h1", WindowType.QueuedPopup);
            var h2 = new TestWindow("h2", WindowType.QueuedPopup);
            manager.Show(h1);
            await IntegrationTestHelpers.WaitFrames(host, 1);
            await manager.Hide(h1, ignoreAnimation: true).WaitForFinish();
            if (h1.IsWindowVisible)
                return IntegrationResult.Fail(Name, "h1 should be hidden before next QueuedPopup Show");

            manager.Show(h2);
            if (h2.Created || manager.QueuedCount != 1)
                return IntegrationResult.Fail(Name,
                    $"After Hide(h1), Show(h2) must enqueue (Created={h2.Created}, QueuedCount={manager.QueuedCount})");
            if (!ReferenceEquals(manager.Current, h1))
                return IntegrationResult.Fail(Name, $"Expected Current=h1 after enqueue h2, actual={Describe(manager.Current)}");

            await manager.Dismiss(h1, ignoreAnimation: true).WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);
            if (manager.Current is not TestWindow shownH2 || shownH2.Tag != "h2")
                return IntegrationResult.Fail(Name, $"Expected h2 shown after Dismiss h1, actual={Describe(manager.Current)}");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            app?.Dispose();
            wm.QueueFree();
            await IntegrationTestHelpers.WaitProcessFrame(host);
        }
    }

    private static string Describe(IWindow? w)
        => w is TestWindow t ? t.Tag : w?.GetType().Name ?? "null";

    private partial class TestWindow : GodotWindow
    {
        public string Tag { get; }

        public TestWindow(string tag, WindowType type)
        {
            Tag = tag;
            WindowName = tag;
            WindowType = type;
            CustomMinimumSize = new Vector2(80, 40);
        }
    }
}
