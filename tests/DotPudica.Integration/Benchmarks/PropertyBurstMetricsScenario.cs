using System.Diagnostics;
using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;
using DotPudica.Godot;
using DotPudica.Godot.Views;
using DotPudica.Integration.Controls;
using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Benchmarks;

public sealed class PropertyBurstMetricsScenario : IBenchmarkScenario
{
    public string Name => "PropertyBurstMetrics";

    public async Task RunAsync(Node host, BenchmarkMetricsCollector metrics)
    {
        foreach (var n in new[] { 1_000, 10_000 })
            metrics.AddPropertyBurst(await MeasureAsync(host, n));
    }

    private static async Task<PropertyBurstMetric> MeasureAsync(Node host, int sourceUpdates)
    {
        var tracking = new ThreadTrackingControl { Name = $"PropertyBurst_{sourceUpdates}" };
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

        var writesBefore = tracking.AccessCount;
        var sw = Stopwatch.StartNew();
        await Task.Run(() =>
        {
            for (var i = 0; i < sourceUpdates; i++)
                viewModel.Title = $"value-{i}";
        });

        var frames = 0;
        var expected = $"value-{sourceUpdates - 1}";
        while (tracking.Text != expected && frames < 120)
        {
            await IntegrationTestHelpers.WaitProcessFrame(host);
            frames++;
        }
        sw.Stop();

        var result = new PropertyBurstMetric
        {
            SourceUpdates = sourceUpdates,
            TargetWrites = tracking.AccessCount - writesBefore,
            FramesToSettle = frames,
            ElapsedMs = sw.Elapsed.TotalMilliseconds,
            FinalValue = tracking.Text,
            Settled = tracking.Text == expected,
        };

        runtime.Dispose();
        tracking.QueueFree();
        await IntegrationTestHelpers.WaitProcessFrame(host);
        return result;
    }
}
