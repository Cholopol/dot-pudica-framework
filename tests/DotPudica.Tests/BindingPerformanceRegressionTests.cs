using System.Collections.ObjectModel;
using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// Regression test: verifies that high-frequency background updates are coalesced into a fixed number of UI work items,
/// without relying on time-based thresholds, to avoid introducing flaky micro-benchmarks in CI.
/// </summary>
public class BindingPerformanceRegressionTests
{
    [Fact]
    public void PropertyUpdateBurst_QueuesOneUiWorkItem_AndAppliesLatestValue()
    {
        var viewModel = new SimpleViewModel { Name = "initial" };
        var proxy = new StubTargetProxy<string>();
        var dispatcher = new QueuedUiDispatcher();
        var path = BindingPathFactory.Create(
            static (SimpleViewModel vm) => vm.Name,
            static (vm, v) => vm.Name = v,
            "Name");
        using var binding = new PropertyBinding<string, string>(proxy, path, BindingMode.OneWay, dispatcher: dispatcher);

        binding.Bind(viewModel);
        dispatcher.HasAccess = false;
        for (var i = 0; i < 1_000; i++)
            viewModel.Name = $"value-{i}";

        Assert.Equal(1, dispatcher.PendingCount);
        dispatcher.RunAll();

        Assert.Equal("value-999", proxy.SetValues.Last());
        Assert.Equal(2, proxy.SetValueCallCount);
        dispatcher.HasAccess = true;
    }

    [Fact]
    public void CollectionUpdateBurst_QueuesOneUiWorkItem_AndPreservesAllChanges()
    {
        var viewModel = new CollectionViewModel();
        var proxy = new StubItemsTargetProxy();
        var dispatcher = new QueuedUiDispatcher();
        var path = BindingPathFactory.Create(
            static (CollectionViewModel vm) => vm.Items,
            static (vm, v) => vm.Items = v,
            "Items");
        using var binding = new CollectionBinding(proxy, path, dispatcher);

        binding.Bind(viewModel);
        dispatcher.RunAll();
        dispatcher.HasAccess = false;
        for (var i = 0; i < 1_000; i++)
            viewModel.Items.Add($"item-{i}");

        Assert.Equal(1, dispatcher.PendingCount);
        dispatcher.RunAll();

        Assert.Equal(1_000, proxy.Items.Count);
        Assert.Equal("item-999", proxy.Items[^1]);
        dispatcher.HasAccess = true;
    }
}
