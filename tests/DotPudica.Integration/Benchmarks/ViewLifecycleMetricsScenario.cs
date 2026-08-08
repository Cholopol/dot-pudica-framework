using System.Diagnostics;
using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;
using DotPudica.Godot;
using DotPudica.Godot.Views;
using DotPudica.Integration.Controls;
using Godot;

namespace DotPudica.Integration.Benchmarks;

public sealed class ViewLifecycleMetricsScenario : IBenchmarkScenario
{
    public string Name => "ViewLifecycleMetrics";

    public async Task RunAsync(Node host, BenchmarkMetricsCollector metrics)
    {
        foreach (var bindCount in new[] { 10, 50, 100 })
            metrics.AddViewLifecycle(await MeasureAsync(host, bindCount));
    }

    private static async Task<ViewLifecycleMetric> MeasureAsync(Node host, int bindCount)
    {
        var controls = new List<ThreadTrackingControl>(bindCount);
        for (var i = 0; i < bindCount; i++)
        {
            var control = new ThreadTrackingControl { Name = $"Life_{bindCount}_{i}" };
            host.AddChild(control);
            controls.Add(control);
        }

        var runtime = new DotPudicaViewRuntime<LifecycleMetricsViewModel>();
        runtime.CaptureUiContext();
        var vm = new LifecycleMetricsViewModel(bindCount);

        var initSw = Stopwatch.StartNew();
        runtime.SetViewModel(vm, ViewModelOwnership.Owned);
        for (var i = 0; i < bindCount; i++)
        {
            var index = i;
            var path = new TypedBindingPath<LifecycleMetricsViewModel, string>(
                x => x.GetValue(index),
                (x, v) => x.SetValue(index, v),
                [$"Value{index}"]);
            var proxy = new DelegateTargetProxy<ThreadTrackingControl, string>(
                controls[i],
                static c => c.Text,
                static (c, v) => c.Text = v);
            runtime.BindProperty(proxy, path, BindingMode.OneWay);
        }
        await IntegrationTestHelpers.WaitFrames(host, 2);
        initSw.Stop();

        var disposeSw = Stopwatch.StartNew();
        runtime.Dispose();
        disposeSw.Stop();

        foreach (var control in controls)
            control.QueueFree();
        await IntegrationTestHelpers.WaitProcessFrame(host);

        return new ViewLifecycleMetric
        {
            BindCount = bindCount,
            InitMs = initSw.Elapsed.TotalMilliseconds,
            DisposeMs = disposeSw.Elapsed.TotalMilliseconds,
        };
    }

    private sealed class LifecycleMetricsViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly string[] _values;

        public LifecycleMetricsViewModel(int count)
        {
            _values = new string[count];
            for (var i = 0; i < count; i++)
                _values[i] = $"v{i}";
        }

        public string GetValue(int index) => _values[index];

        public void SetValue(int index, string value)
        {
            if (_values[index] == value)
                return;
            _values[index] = value;
            OnPropertyChanged($"Value{index}");
        }
    }
}
