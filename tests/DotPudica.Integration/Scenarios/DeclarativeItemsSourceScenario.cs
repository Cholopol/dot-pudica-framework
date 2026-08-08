using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Declarative [ItemsSource] full pipeline: source-generated binding + real container item instantiation,
/// add/remove sync, pooling without node accumulation, and ItemCommand bubbling from row button to host ViewModel.
/// </summary>
public sealed class DeclarativeItemsSourceScenario : IIntegrationScenario
{
    public string Name => "Declarative_ItemsSource_WithItemCommand";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var view = new DeclarativeItemsSourceView();
        host.AddChild(view);
        await IntegrationTestHelpers.WaitFrames(host, 2);

        try
        {
            var vm = view.PanelViewModel;
            if (vm is null)
                return IntegrationResult.Fail(Name, "ViewModel was not created by the generated factory");
            if (view.List is null)
                return IntegrationResult.Fail(Name, "Items list container was not created by OnViewReady");

            // Initial sync: add items after binding established
            vm.Items.Add("alpha");
            vm.Items.Add("beta");
            await IntegrationTestHelpers.WaitFrames(host, 2);

            if (view.List.GetChildCount() != 2)
                return IntegrationResult.Fail(Name, $"Child count after Add={view.List.GetChildCount()}, expected 2");

            // ItemCommand bubbling: press the row button, host VM should record the row item
            var row = view.List.GetChild(1);
            var button = row.FindChild("SelectButton", recursive: false, owned: false) as Button;
            if (button is null)
                return IntegrationResult.Fail(Name, "Row SelectButton not found");
            button.EmitSignal(BaseButton.SignalName.Pressed);
            await IntegrationTestHelpers.WaitFrames(host, 1);

            if (vm.SelectedItems.Count != 1 || vm.SelectedItems[0] != "beta")
                return IntegrationResult.Fail(Name, $"ItemCommand bubbling expected ['beta'], actual=[{string.Join(",", vm.SelectedItems)}]");

            // Remove sync
            vm.Items.RemoveAt(0);
            await IntegrationTestHelpers.WaitFrames(host, 2);
            if (view.List.GetChildCount() != 1)
                return IntegrationResult.Fail(Name, $"Child count after Remove={view.List.GetChildCount()}, expected 1");

            // Pooling: clear + refill with more items than pool size, children must equal collection count (no orphan accumulation)
            vm.Items.Clear();
            await IntegrationTestHelpers.WaitFrames(host, 2);
            for (var i = 0; i < 7; i++)
                vm.Items.Add($"n{i}");
            await IntegrationTestHelpers.WaitFrames(host, 2);

            if (view.List.GetChildCount() != 7)
                return IntegrationResult.Fail(Name, $"Child count after pool reuse={view.List.GetChildCount()}, expected 7");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            host.RemoveChild(view);
            view.QueueFree();
        }
    }
}
