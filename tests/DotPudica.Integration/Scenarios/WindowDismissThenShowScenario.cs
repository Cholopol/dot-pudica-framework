using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Navigate-style: animated Dismiss of Full A then immediate Show of Full B must not
/// Hide/Cancel A's dismiss (which would drop WindowDismissed and leave a zombie on the stack).
/// </summary>
public sealed partial class WindowDismissThenShowScenario : IIntegrationScenario
{
    public string Name => "WindowDismiss_ThenShowDoesNotCancelDismiss";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var wm = new GodotWindowManager { Name = "TestWindowManager_DismissThenShow" };
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
            var pageA = new TestWindow("page-a", WindowType.Full);
            var pageB = new TestWindow("page-b", WindowType.Full);
            var aDismissed = 0;
            pageA.WindowDismissed += (_, _) => aDismissed++;

            manager.Show(pageA);
            await IntegrationTestHelpers.WaitFrames(host, 1);

            // Start animated dismiss, then immediately show next Full (Showcase Navigate pattern).
            pageA.Dismiss();
            manager.Show(pageB);
            await IntegrationTestHelpers.WaitFrames(host, 30);

            if (aDismissed != 1)
                return IntegrationResult.Fail(Name,
                    $"Expected page-a WindowDismissed once, got {aDismissed}");

            if (manager.Current is not TestWindow cur || cur.Tag != "page-b")
                return IntegrationResult.Fail(Name,
                    $"Expected Current=page-b, actual={Describe(manager.Current)}");

            if (GodotObject.IsInstanceValid(pageA) && !pageA.Dismissed)
                return IntegrationResult.Fail(Name, "page-a should be dismissed (no zombie)");

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
