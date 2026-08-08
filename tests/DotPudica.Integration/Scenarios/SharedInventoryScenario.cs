using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using DotPudica.Core.ViewModels;
using DotPudica.Godot;
using DotPudica.Godot.Views;
using Godot;
using DotPudica.Integration.Controls;

namespace DotPudica.Integration.Scenarios;

/// <summary>Inventory + hotbar share InventoryViewModel: hotbar still responds after bag closes; owner only disposes once.</summary>
public sealed class SharedInventoryScenario : IIntegrationScenario
{
    public string Name => "SharedInventory_SurvivesBagClose";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var session = new InventorySession();
        var bag = new InventoryPanelView { Name = "Bag" };
        var hotbar = new InventoryPanelView { Name = "Hotbar" };
        host.AddChild(bag);
        host.AddChild(hotbar);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        bag.BindShared(session.Inventory);
        hotbar.BindShared(session.Inventory);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        session.Inventory.AddItem("sword");
        await IntegrationTestHelpers.WaitProcessFrame(host);

        if (bag.Tracking.Text != "sword" || hotbar.Tracking.Text != "sword")
            return IntegrationResult.Fail(Name, "Shared VM did not sync to both Views");

        var hotbarAccess = hotbar.Tracking.AccessCount;
        bag.QueueFree();
        await IntegrationTestHelpers.WaitFrames(host, 2);

        session.Inventory.AddItem("potion");
        await IntegrationTestHelpers.WaitProcessFrame(host);

        if (hotbar.Tracking.AccessCount <= hotbarAccess)
            return IntegrationResult.Fail(Name, "Hotbar did not continue responding to inventory changes after bag closed");

        if (!hotbar.Tracking.Text.Contains("potion", StringComparison.Ordinal))
            return IntegrationResult.Fail(Name, $"Hotbar did not show potion, actual={hotbar.Tracking.Text}");

        if (session.Inventory.IsDisposed)
            return IntegrationResult.Fail(Name, "Shared InventoryViewModel was incorrectly disposed when closing the bag View");

        session.Dispose();
        if (!session.Inventory.IsDisposed)
            return IntegrationResult.Fail(Name, "InventoryViewModel was not disposed after session owner released");

        hotbar.QueueFree();
        await IntegrationTestHelpers.WaitProcessFrame(host);
        return IntegrationResult.Pass(Name);
    }
}

public sealed class InventorySession : IDisposable
{
    public InventoryViewModel Inventory { get; } = new();

    public void Dispose() => Inventory.Dispose();
}

public partial class InventoryViewModel : ViewModelBase
{
    public ObservableCollection<string> Items { get; } = new();

    [ObservableProperty]
    private string _summary = "";

    public void AddItem(string item)
    {
        Items.Add(item);
        Summary = string.Join(",", Items);
    }
}

public partial class InventoryPanelView : Control
{
    private readonly DotPudicaViewRuntime<InventoryViewModel> _runtime = new();
    public ThreadTrackingControl Tracking { get; private set; } = null!;

    public void BindShared(InventoryViewModel shared)
    {
        Tracking = new ThreadTrackingControl { Name = $"{Name}_Tracking" };
        AddChild(Tracking);
        _runtime.CaptureUiContext();
        // Shared VM must be External
        _runtime.SetViewModel(shared, ViewModelOwnership.External);
        var path = new TypedBindingPath<InventoryViewModel, string>(
            static x => x.Summary,
            static (x, v) => x.Summary = v,
            ["Summary"]);
        var proxy = new DelegateTargetProxy<ThreadTrackingControl, string>(
            Tracking,
            static c => c.Text,
            static (c, v) => c.Text = v);
        _runtime.BindProperty(proxy, path, BindingMode.OneWay);
    }

    public override void _ExitTree()
    {
        _runtime.Dispose();
        base._ExitTree();
    }
}
