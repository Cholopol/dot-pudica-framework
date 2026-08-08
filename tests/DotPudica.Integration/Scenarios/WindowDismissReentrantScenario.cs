using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Repeated Dismiss() during an in-flight close must not cancel WindowDismissed / stack pop
/// (the Progress gallery bug: per-frame Dismiss after 100%).
/// </summary>
public sealed partial class WindowDismissReentrantScenario : IIntegrationScenario
{
    public string Name => "WindowDismiss_ReentrantDoesNotDropOnEnd";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var wm = new GodotWindowManager { Name = "TestWindowManager_DismissReentrant" };
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
            var popup = new TestWindow("reentrant-popup", WindowType.Popup);
            var dismissed = 0;
            popup.WindowDismissed += (_, _) => dismissed++;

            manager.Show(popup);
            await IntegrationTestHelpers.WaitFrames(host, 1);

            if (!ReferenceEquals(manager.Current, popup))
                return IntegrationResult.Fail(Name, $"Expected Current=popup before dismiss, actual={Describe(manager.Current)}");

            // First call starts the animated dismiss; subsequent calls must reuse it.
            var dismiss = popup.Dismiss();
            for (var i = 0; i < 12; i++)
                popup.Dismiss();

            await dismiss.WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);

            if (dismissed != 1)
                return IntegrationResult.Fail(Name,
                    $"Expected WindowDismissed exactly once, got {dismissed} (reentrant Dismiss likely canceled OnEnd)");

            if (manager.Current is not null)
                return IntegrationResult.Fail(Name,
                    $"Expected stack empty after dismiss, Current={Describe(manager.Current)}");

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
