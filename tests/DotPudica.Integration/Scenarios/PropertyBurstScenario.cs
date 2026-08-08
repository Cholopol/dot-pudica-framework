using DotPudica.Core.ViewModels;
using DotPudica.Core.Binding;
using DotPudica.Godot;
using DotPudica.Godot.Views;
using Godot;
using DotPudica.Integration.Controls;
using DotPudica.Integration.Fixtures;

namespace DotPudica.Integration.Scenarios;

/// <summary>High-frequency background property writes should only coalesce the latest value into a limited number of main thread target updates.</summary>
public sealed class PropertyBurstScenario : IIntegrationScenario
{
    public string Name => "PropertyBurst_CoalescesUiUpdates";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var tracking = new ThreadTrackingControl { Name = "PropertyBurstTracking" };
        host.AddChild(tracking);

        var runtime = new DotPudicaViewRuntime<IntegrationTitleViewModel>();
        runtime.CaptureUiContext();
        var viewModel = new IntegrationTitleViewModel { Title = "initial" };
        runtime.SetViewModel(viewModel, ViewModelOwnership.External);
        var path = new TypedBindingPath<IntegrationTitleViewModel, string>(
            static vm => vm.Title,
            static (vm, v) => vm.Title = v,
            ["Title"]);
        var proxy = new DelegateTargetProxy<ThreadTrackingControl, string>(
            tracking,
            static c => c.Text,
            static (c, v) => c.Text = v);
        runtime.BindProperty(proxy, path, BindingMode.OneWay);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var writesBeforeBurst = tracking.AccessCount;
        await Task.Run(() =>
        {
            for (var i = 0; i < 1_000; i++)
                viewModel.Title = $"value-{i}";
        });
        await IntegrationTestHelpers.WaitFrames(host, 3);

        try
        {
            if (tracking.Text != "value-999")
                return IntegrationResult.Fail(Name, $"Expected final Text=value-999, actual={tracking.Text}");

            var burstWrites = tracking.AccessCount - writesBeforeBurst;
            if (burstWrites > 2)
                return IntegrationResult.Fail(Name, $"1000 background updates triggered {burstWrites} target writes, not effectively coalesced");

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
