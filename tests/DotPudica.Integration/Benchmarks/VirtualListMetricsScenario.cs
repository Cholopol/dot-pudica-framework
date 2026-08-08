using System.Collections.ObjectModel;
using System.Diagnostics;
using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Binding.ControlProxies;
using DotPudica.Godot.Views;
using Godot;

namespace DotPudica.Integration.Benchmarks;

public sealed class VirtualListMetricsScenario : IBenchmarkScenario
{
    private const string ItemScene = "res://tests/DotPudica.Integration/Fixtures/IntegrationItem.tscn";
    private const string VirtualItemScene = "res://samples/Showcase/Gallery/VirtualList/VirtualListItem.tscn";

    public string Name => "VirtualListMetrics";

    public async Task RunAsync(Node host, BenchmarkMetricsCollector metrics)
    {
        foreach (var n in new[] { 100, 500, 1_000 })
            metrics.AddVirtualList(await MeasureNonVirtualAsync(host, n));

        foreach (var n in new[] { 1_000, 10_000, 50_000 })
            metrics.AddVirtualList(await MeasureVirtualAsync(host, n));
    }

    private static async Task<VirtualListMetric> MeasureNonVirtualAsync(Node host, int itemCount)
    {
        var container = new VBoxContainer
        {
            Name = $"NonVirtual_{itemCount}",
            CustomMinimumSize = new Vector2(400, 320),
            Size = new Vector2(400, 320),
        };
        host.AddChild(container);

        var runtime = new DotPudicaViewRuntime<ListMetricsViewModel>();
        runtime.CaptureUiContext();
        var vm = new ListMetricsViewModel();
        for (var i = 0; i < itemCount; i++)
            vm.Items.Add($"item-{i}");

        var sw = Stopwatch.StartNew();
        runtime.SetViewModel(vm, ViewModelOwnership.Owned);
        runtime.BindItems(
            container,
            ItemScene,
            new TypedBindingPath<ListMetricsViewModel, ObservableCollection<string>>(
                static x => x.Items,
                null,
                ["Items"]));
        await IntegrationTestHelpers.WaitFrames(host, 3);
        sw.Stop();
        var bindMs = sw.Elapsed.TotalMilliseconds;
        var active = container.GetChildCount();

        sw.Restart();
        await IntegrationTestHelpers.WaitFrames(host, 2);
        sw.Stop();

        var result = new VirtualListMetric
        {
            Mode = "non-virtual",
            ItemCount = itemCount,
            ActiveNodes = active,
            BindMs = bindMs,
            ScrollMs = sw.Elapsed.TotalMilliseconds,
        };

        runtime.Dispose();
        container.QueueFree();
        await IntegrationTestHelpers.WaitProcessFrame(host);
        return result;
    }

    private static async Task<VirtualListMetric> MeasureVirtualAsync(Node host, int itemCount)
    {
        var list = new VirtualizedItemsControl
        {
            Name = $"Virtual_{itemCount}",
            ItemHeight = 32,
            Overscan = 1,
            CustomMinimumSize = new Vector2(400, 320),
            Size = new Vector2(400, 320),
        };
        host.AddChild(list);

        var runtime = new DotPudicaViewRuntime<ListMetricsViewModel>();
        runtime.CaptureUiContext();
        var vm = new ListMetricsViewModel();
        for (var i = 0; i < itemCount; i++)
            vm.Items.Add($"item-{i}");

        var sw = Stopwatch.StartNew();
        runtime.SetViewModel(vm, ViewModelOwnership.Owned);
        runtime.BindVirtualizedItems(
            list,
            VirtualItemScene,
            new TypedBindingPath<ListMetricsViewModel, ObservableCollection<string>>(
                static x => x.Items,
                null,
                ["Items"]));
        await IntegrationTestHelpers.WaitFrames(host, 3);
        sw.Stop();
        var bindMs = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        list.ScrollToIndex(Math.Min(itemCount / 2, itemCount - 1));
        await IntegrationTestHelpers.WaitFrames(host, 3);
        sw.Stop();

        var result = new VirtualListMetric
        {
            Mode = "virtual",
            ItemCount = itemCount,
            ActiveNodes = list.ActiveItemCount,
            BindMs = bindMs,
            ScrollMs = sw.Elapsed.TotalMilliseconds,
        };

        runtime.Dispose();
        list.QueueFree();
        await IntegrationTestHelpers.WaitProcessFrame(host);
        return result;
    }

    private sealed class ListMetricsViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public ObservableCollection<string> Items { get; } = new();
    }
}
