using System.Collections.ObjectModel;
using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Binding.ControlProxies;
using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Real ItemsSource container full pipeline: ContainerItemsProxy + item scene instantiation → child node count / content synchronization.
/// </summary>
public sealed class ItemsSourceContainerScenario : IIntegrationScenario
{
    public string Name => "ItemsSource_ContainerInstantiatesItemScenes";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);

        var itemScene = GD.Load<PackedScene>("res://tests/DotPudica.Integration/Fixtures/IntegrationItem.tscn");
        if (itemScene is null)
            return IntegrationResult.Fail(Name, "Failed to load IntegrationItem.tscn");

        var container = new VBoxContainer { Name = "ItemsContainer" };
        host.AddChild(container);

        var proxy = new ContainerItemsProxy(container, itemScene, poolSize: 4);
        var context = new BindingContext();
        context.SetUiDispatcher(dispatcher);

        var vm = new ItemsContainerFixtureViewModel();
        var path = new TypedBindingPath<ItemsContainerFixtureViewModel, ObservableCollection<string>>(
            static x => x.Items,
            null,
            ["Items"]);
        context.AddBinding(new CollectionBinding(proxy, path, dispatcher));
        context.DataContext = vm;
        await IntegrationTestHelpers.WaitProcessFrame(host);

        try
        {
            vm.Items.Add("alpha");
            vm.Items.Add("beta");
            vm.Items.Add("gamma");
            await IntegrationTestHelpers.WaitFrames(host, 2);

            if (container.GetChildCount() != 3)
                return IntegrationResult.Fail(Name, $"Child count after Add={container.GetChildCount()}, expected 3");

            if (!TryReadItemText(container.GetChild(1), out var mid) || mid != "beta")
                return IntegrationResult.Fail(Name, $"Item 1 text expected beta, actual={mid}");

            vm.Items.RemoveAt(0);
            await IntegrationTestHelpers.WaitFrames(host, 2);

            if (container.GetChildCount() != 2)
                return IntegrationResult.Fail(Name, $"Child count after Remove={container.GetChildCount()}, expected 2");

            if (!TryReadItemText(container.GetChild(0), out var first) || first != "beta")
                return IntegrationResult.Fail(Name, $"First item after removal expected beta, actual={first}");

            // Pooling: repeatedly clearing and refilling should not accumulate orphaned nodes infinitely (container child count should equal collection count)
            vm.Items.Clear();
            await IntegrationTestHelpers.WaitFrames(host, 2);
            for (var i = 0; i < 8; i++)
                vm.Items.Add($"n{i}");
            await IntegrationTestHelpers.WaitFrames(host, 2);

            if (container.GetChildCount() != 8)
                return IntegrationResult.Fail(Name, $"Child count after pool reuse={container.GetChildCount()}, expected 8");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            context.Dispose();
            container.QueueFree();
        }
    }

    private static bool TryReadItemText(Node node, out string text)
    {
        text = "";
        if (node is not IItemsControlItem item)
            return false;
        text = item.DataContext?.ToString() ?? "";
        return true;
    }

    private sealed class ItemsContainerFixtureViewModel : ViewModelBase
    {
        public ObservableCollection<string> Items { get; } = new();
    }
}
