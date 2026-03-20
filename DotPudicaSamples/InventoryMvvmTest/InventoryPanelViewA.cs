using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using Godot;

namespace Samples.InventoryMvvmTest;

public partial class InventoryPanelViewA : InventoryPanelView
{
    [Export, BindTo(nameof(InventoryTestViewModel.ViewACellSize), Mode = BindingMode.TwoWay)]
    private SpinBox _cellSizeEditor = null!;

    [Export, BindTo(nameof(InventoryTestViewModel.SyncCellSize), Mode = BindingMode.TwoWay)]
    private CheckBox _syncToggle = null!;

    protected override SpinBox CellSizeEditor => _cellSizeEditor;
    protected override int GetCellSize(InventoryTestViewModel vm) => vm.ViewACellSize;
    protected override string PanelTitle => "View A";
}
