using DotPudica.Core.ViewModels;
using DotPudica.Core.Binding;
using DotPudica.Godot;
using DotPudica.Godot.Views;
using Godot;
using DotPudica.Integration.Controls;
using DotPudica.Integration.Fixtures;

namespace DotPudica.Integration.Scenarios;

/// <summary>After consecutive DataContext switches, stale posted work must not overwrite the latest ViewModel value.</summary>
public sealed class StaleWorkScenario : IIntegrationScenario
{
    public string Name => "Rebind_DropsStalePostedWork";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var tracking = new ThreadTrackingControl { Name = "StaleTracking" };
        host.AddChild(tracking);

        var runtime = new DotPudicaViewRuntime<IntegrationTitleViewModel>();
        runtime.CaptureUiContext();

        var first = new IntegrationTitleViewModel { Title = "first" };
        var second = new IntegrationTitleViewModel { Title = "second" };
        runtime.SetViewModel(first, ViewModelOwnership.External);
        var path = new TypedBindingPath<IntegrationTitleViewModel, string>(
            static x => x.Title,
            static (x, v) => x.Title = v,
            ["Title"]);
        var proxy = new DelegateTargetProxy<ThreadTrackingControl, string>(
            tracking,
            static c => c.Text,
            static (c, v) => c.Text = v);
        runtime.BindProperty(proxy, path, BindingMode.OneWay);

        // Flood the first VM from a worker, then immediately rebind to second.
        var flood = Task.Run(() =>
        {
            for (var i = 0; i < 32; i++)
                first.Title = $"stale-{i}";
        });

        runtime.SetViewModel(second, ViewModelOwnership.External);
        await flood;
        await IntegrationTestHelpers.WaitFrames(host, 3);

        try
        {
            if (tracking.Text != "second")
                return IntegrationResult.Fail(Name, $"Expected final Text=second, actual={tracking.Text}");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            runtime.Dispose();
            tracking.QueueFree();
            await IntegrationTestHelpers.WaitProcessFrame(host);
        }
    }
}
