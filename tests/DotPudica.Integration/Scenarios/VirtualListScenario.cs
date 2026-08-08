using DotPudica.Core.ViewModels;
using System.Collections.ObjectModel;
using DotPudica.Core.Binding;
using DotPudica.Godot.Binding.ControlProxies;
using DotPudica.Godot.Views;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>Virtual list active node count does not grow linearly with total item count.</summary>
public sealed class VirtualListScenario : IIntegrationScenario
{
    public string Name => "VirtualList_ReusesVisibleNodes";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        const int totalItems = 10_000;
        var list = new VirtualizedItemsControl
        {
            Name = "VirtualList",
            ItemHeight = 32,
            Overscan = 1,
            CustomMinimumSize = new Vector2(400, 320),
            Size = new Vector2(400, 320),
        };
        host.AddChild(list);

        var runtime = new DotPudicaViewRuntime<VirtualListScenarioViewModel>();
        runtime.CaptureUiContext();
        var vm = new VirtualListScenarioViewModel();
        for (var i = 0; i < totalItems; i++)
            vm.Items.Add($"item-{i}");

        runtime.SetViewModel(vm, ViewModelOwnership.Owned);
        runtime.BindVirtualizedItems(
            list,
            "res://samples/Showcase/Gallery/VirtualList/VirtualListItem.tscn",
            new TypedBindingPath<VirtualListScenarioViewModel, ObservableCollection<string>>(
                static x => x.Items,
                null,
                ["Items"]));

        await IntegrationTestHelpers.WaitFrames(host, 2);
        list.ScrollToIndex(5000);
        await IntegrationTestHelpers.WaitFrames(host, 2);

        try
        {
            var active = list.ActiveItemCount;
            var maxExpected = (int)Math.Ceiling(list.Size.Y / list.ItemHeight) + list.Overscan * 2 + 4;
            if (active <= 0)
                return IntegrationResult.Fail(Name, "Virtual list did not create any active nodes");

            if (active > maxExpected)
                return IntegrationResult.Fail(Name,
                    $"Active node count {active} is too large (upper bound ~{maxExpected}), suspected not reusing");

            if (active >= totalItems / 10)
                return IntegrationResult.Fail(Name,
                    $"Active node count {active} grows linearly with total items (total={totalItems})");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            runtime.Dispose();
            list.QueueFree();
            await IntegrationTestHelpers.WaitProcessFrame(host);
        }
    }
}

public sealed class VirtualListScenarioViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public ObservableCollection<string> Items { get; } = new();
}
