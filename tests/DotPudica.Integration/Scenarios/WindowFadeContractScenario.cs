using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Fade-out contract: animated Dismiss keeps Visible + IsDismissing until OnEnd;
/// ignoreAnimation snaps modulate.a to 0.
/// </summary>
public sealed partial class WindowFadeContractScenario : IIntegrationScenario
{
    public string Name => "WindowFade_DismissContract";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var wm = new GodotWindowManager { Name = "TestWindowManager_FadeContract" };
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

            var animated = new TestWindow("fade-animated", WindowType.Popup);
            manager.Show(animated);
            await IntegrationTestHelpers.WaitFrames(host, 1);
            await animated.Show(ignoreAnimation: true).WaitForFinish();

            if (animated.Modulate.A < 0.99f)
                return IntegrationResult.Fail(Name, $"Expected opaque after show snap, a={animated.Modulate.A}");

            var dismiss = animated.Dismiss(ignoreAnimation: false);
            await IntegrationTestHelpers.WaitFrames(host, 1);

            if (!GodotObject.IsInstanceValid(animated))
                return IntegrationResult.Fail(Name, "Animated dismiss finished too early (node freed before fade)");

            if (!animated.IsDismissing)
                return IntegrationResult.Fail(Name, "Expected IsDismissing during animated dismiss");

            if (!animated.Visible)
                return IntegrationResult.Fail(Name, "Expected Visible=true during fade-out (OnEnd hides)");

            await dismiss.WaitForFinish();
            await IntegrationTestHelpers.WaitFrames(host, 1);

            if (GodotObject.IsInstanceValid(animated) && !animated.Dismissed)
                return IntegrationResult.Fail(Name, "Expected animated dismiss to finish and free/dismiss the window");

            var snapped = new TestWindow("fade-snap", WindowType.Popup);
            manager.Show(snapped);
            await IntegrationTestHelpers.WaitFrames(host, 1);
            await snapped.Show(ignoreAnimation: true).WaitForFinish();

            var beforeA = snapped.Modulate.A;
            if (beforeA < 0.99f)
                return IntegrationResult.Fail(Name, $"Expected opaque before snap dismiss, a={beforeA}");

            // Capture alpha after OnStart/ApplyAlpha but before QueueFree frees the node:
            // ignoreAnimation completes synchronously inside Dismiss.
            float snappedA = -1f;
            var dismissed = false;
            snapped.WindowDismissed += (_, _) =>
            {
                dismissed = true;
                if (GodotObject.IsInstanceValid(snapped))
                    snappedA = snapped.Modulate.A;
            };

            manager.Dismiss(snapped, ignoreAnimation: true);

            if (!dismissed)
                return IntegrationResult.Fail(Name, "Expected WindowDismissed from ignoreAnimation dismiss");

            if (snappedA > 0.01f)
                return IntegrationResult.Fail(Name, $"Expected modulate.a snapped to 0, got {snappedA}");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            app?.Dispose();
            wm.QueueFree();
            await IntegrationTestHelpers.WaitProcessFrame(host);
        }
    }

    private partial class TestWindow : GodotWindow
    {
        public TestWindow(string tag, WindowType type)
        {
            WindowName = tag;
            WindowType = type;
            CustomMinimumSize = new Vector2(80, 40);
        }
    }
}
