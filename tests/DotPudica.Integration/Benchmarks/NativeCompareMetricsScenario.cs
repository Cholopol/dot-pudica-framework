using System.ComponentModel;
using System.Diagnostics;
using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;
using DotPudica.Godot;
using DotPudica.Godot.Views;
using DotPudica.Integration.Controls;
using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Benchmarks;

public sealed class NativeCompareMetricsScenario : IBenchmarkScenario
{
    public string Name => "NativeCompareMetrics";

    public async Task RunAsync(Node host, BenchmarkMetricsCollector metrics)
    {
        foreach (var n in new[] { 1_000, 10_000 })
        {
            metrics.AddNativeCompare(await MeasureNativeDirectAsync(host, n));
            metrics.AddNativeCompare(await MeasureBoundMainThreadAsync(host, n));
            metrics.AddNativeCompare(await MeasureCoalescedAsync(host, n));
        }
    }

    private static async Task<NativeCompareMetric> MeasureNativeDirectAsync(Node host, int sourceUpdates)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);

        var tracking = new ThreadTrackingControl { Name = $"NativeDirect_{sourceUpdates}" };
        host.AddChild(tracking);

        var vm = new IntegrationTitleViewModel { Title = "initial" };
        void OnChanged(object? _, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IntegrationTitleViewModel.Title))
                return;
            var value = vm.Title;
            dispatcher.Post(() => tracking.Text = value);
        }

        vm.PropertyChanged += OnChanged;
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var writesBefore = tracking.AccessCount;
        var sw = Stopwatch.StartNew();
        await Task.Run(() =>
        {
            for (var i = 0; i < sourceUpdates; i++)
                vm.Title = $"value-{i}";
        });

        var expected = $"value-{sourceUpdates - 1}";
        var frames = await WaitUntilAsync(host, () => tracking.Text == expected);
        sw.Stop();

        var result = new NativeCompareMetric
        {
            Mode = "native-direct",
            SourceUpdates = sourceUpdates,
            TargetWrites = tracking.AccessCount - writesBefore,
            FramesToSettle = frames,
            ElapsedMs = sw.Elapsed.TotalMilliseconds,
            FinalValue = tracking.Text,
            Settled = tracking.Text == expected,
        };

        vm.PropertyChanged -= OnChanged;
        tracking.QueueFree();
        await IntegrationTestHelpers.WaitProcessFrame(host);
        return result;
    }

    private static async Task<NativeCompareMetric> MeasureBoundMainThreadAsync(Node host, int sourceUpdates)
    {
        var tracking = new ThreadTrackingControl { Name = $"BoundMain_{sourceUpdates}" };
        host.AddChild(tracking);

        var runtime = new DotPudicaViewRuntime<IntegrationTitleViewModel>();
        runtime.CaptureUiContext();
        var vm = new IntegrationTitleViewModel { Title = "initial" };
        runtime.SetViewModel(vm, ViewModelOwnership.External);
        BindTitle(runtime, tracking);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var writesBefore = tracking.AccessCount;
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < sourceUpdates; i++)
            vm.Title = $"value-{i}";
        await IntegrationTestHelpers.WaitProcessFrame(host);
        sw.Stop();

        var expected = $"value-{sourceUpdates - 1}";
        var result = new NativeCompareMetric
        {
            Mode = "dotpudica-bound",
            SourceUpdates = sourceUpdates,
            TargetWrites = tracking.AccessCount - writesBefore,
            FramesToSettle = 0,
            ElapsedMs = sw.Elapsed.TotalMilliseconds,
            FinalValue = tracking.Text,
            Settled = tracking.Text == expected,
        };

        runtime.Dispose();
        tracking.QueueFree();
        await IntegrationTestHelpers.WaitProcessFrame(host);
        return result;
    }

    private static async Task<NativeCompareMetric> MeasureCoalescedAsync(Node host, int sourceUpdates)
    {
        var tracking = new ThreadTrackingControl { Name = $"Coalesced_{sourceUpdates}" };
        host.AddChild(tracking);

        var runtime = new DotPudicaViewRuntime<IntegrationTitleViewModel>();
        runtime.CaptureUiContext();
        var vm = new IntegrationTitleViewModel { Title = "initial" };
        runtime.SetViewModel(vm, ViewModelOwnership.External);
        BindTitle(runtime, tracking);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var writesBefore = tracking.AccessCount;
        var sw = Stopwatch.StartNew();
        await Task.Run(() =>
        {
            for (var i = 0; i < sourceUpdates; i++)
                vm.Title = $"value-{i}";
        });

        var expected = $"value-{sourceUpdates - 1}";
        var frames = await WaitUntilAsync(host, () => tracking.Text == expected);
        sw.Stop();

        var result = new NativeCompareMetric
        {
            Mode = "dotpudica-coalesced",
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

    private static void BindTitle(
        DotPudicaViewRuntime<IntegrationTitleViewModel> runtime,
        ThreadTrackingControl tracking)
    {
        var path = new TypedBindingPath<IntegrationTitleViewModel, string>(
            static vm => vm.Title,
            static (vm, v) => vm.Title = v,
            ["Title"]);
        var proxy = new DelegateTargetProxy<ThreadTrackingControl, string>(
            tracking,
            static c => c.Text,
            static (c, v) => c.Text = v);
        runtime.BindProperty(proxy, path, BindingMode.OneWay);
    }

    private static async Task<int> WaitUntilAsync(Node host, Func<bool> predicate, int maxFrames = 120)
    {
        var frames = 0;
        while (!predicate() && frames < maxFrames)
        {
            await IntegrationTestHelpers.WaitProcessFrame(host);
            frames++;
        }

        return frames;
    }
}
