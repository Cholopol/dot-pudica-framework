using DotPudica.Godot.Views;
using Godot;

namespace Samples.InventoryMvvmTest;

[DotPudicaView(typeof(InventoryTestViewModel))]
public partial class InventoryTestHostView : Control
{
    [Export]
    private InventoryPanelView _viewA = null!;

    [Export]
    private InventoryPanelView _viewB = null!;

    public override void _Ready()
    {
        ViewModel = new InventoryTestViewModel();
        _viewA.ViewModel = ViewModel;
        _viewB.ViewModel = ViewModel;
        DotPudicaInitialize();
    }

    public override void _ExitTree()
    {
        DotPudicaDispose();
        base._ExitTree();
    }
}
