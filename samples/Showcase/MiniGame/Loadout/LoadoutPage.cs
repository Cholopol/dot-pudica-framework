using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;
using Samples.Showcase.MiniGame.Match;

namespace Samples.Showcase.MiniGame.Loadout;

/// <summary>
/// Loadout page shell: owns the single LoadoutViewModel; three child panels share External binding.
/// </summary>
[DotPudicaView(typeof(LoadoutViewModel))]
public partial class LoadoutPage : ShowcasePageWindow
{
    private LoadoutBagPanel _bagPanel = null!;
    private LoadoutEquipmentPanel _equipmentPanel = null!;
    private LoadoutStatsPanel _statsPanel = null!;

    [Export, BindCommand(nameof(LoadoutViewModel.EnterMatchCommand))]
    private Button _enterMatchButton = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    partial void OnViewModelBound()
    {
        _bagPanel.BindShared(ViewModel!);
        _equipmentPanel.BindShared(ViewModel!);
        _statsPanel.BindShared(ViewModel!);
    }

    [Subscribe("EnterMatchRequest.Raised")]
    private void OnEnterMatchRequested(object? sender, EventArgs e)
    {
        var match = new MatchPage { WindowName = "Match" };
        ShowFullPage(match);
        RequireWindowManager().Dismiss(this);
    }

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "Three views share one LoadoutViewModel.");

        var columns = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        columns.AddThemeConstantOverride("separation", 16);
        root.AddChild(columns);

        _bagPanel = new LoadoutBagPanel();
        columns.AddChild(ShowcaseUi.CreateCard(_bagPanel));

        var right = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(320, 0)
        };
        right.AddThemeConstantOverride("separation", 12);
        columns.AddChild(right);

        _equipmentPanel = new LoadoutEquipmentPanel();
        right.AddChild(ShowcaseUi.CreateCard(_equipmentPanel));

        _statsPanel = new LoadoutStatsPanel();
        right.AddChild(ShowcaseUi.CreateCard(_statsPanel));

        _enterMatchButton = ShowcaseUi.CreatePrimaryButton("Start Match with Loadout");
        root.AddChild(_enterMatchButton);
    }
}
