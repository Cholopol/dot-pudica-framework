using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.Threading;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Samples.Showcase.MiniGame.Battle;
using Samples.Showcase.Shared.Models;
using Samples.Showcase.Shared.Services;
using AppContext = DotPudica.Godot.AppContext;

namespace Samples.Showcase.MiniGame.Match;

/// <summary>Match page with SceneOperationScope cancel.</summary>
[DotPudicaView(typeof(MatchViewModel))]
public partial class MatchPage : ShowcasePageWindow
{
    private readonly SceneOperationScope _sceneScope = new();

    [Inject]
    private IShowcaseMatchService _matchService = null!;

    [Export, BindTo(nameof(MatchViewModel.StatusText))]
    private Label _statusLabel = null!;

    [Export, BindTo(nameof(MatchViewModel.RoomId))]
    private Label _roomLabel = null!;

    [Export, BindTo(nameof(MatchViewModel.ErrorText))]
    private Label _errorLabel = null!;

    [Export, BindCommand(nameof(MatchViewModel.MatchCommand))]
    private Button _matchButton = null!;

    [ViewModelFactory]
    private MatchViewModel CreateMatchViewModel()
    {
        var dispatcher = UiDispatcher.FromSynchronizationContext(
            Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext"));
        return new MatchViewModel(_matchService, _sceneScope, dispatcher);
    }

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    partial void OnViewModelBound()
    {
        ViewModel!.MatchCommand.Execute(null);
    }

    partial void OnViewDisposing()
    {
        _sceneScope.Cancel();
        _sceneScope.Dispose();
    }

    [Subscribe(nameof(MatchViewModel.MatchSucceeded))]
    private void OnMatchSucceeded(ShowcaseMatchResult result)
    {
        var inventory = AppContext.Current.Services.GetRequiredService<IInventoryService>();
        var battle = new BattlePage { WindowName = "Battle" };
        battle.SetCombatStats(inventory.Stats);
        ShowFullPage(battle);
        RequireWindowManager().Dismiss(this);
    }

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(center);

        var cardBody = ShowcaseUi.CreateCardBody(out var panel);
        panel.CustomMinimumSize = new Vector2(360, 0);
        center.AddChild(panel);

        ShowcaseUi.AddSection(cardBody, "Matchmaking");

        var metrics = ShowcaseUi.AddMetricsRow(cardBody);
        ShowcaseUi.AddMetricChip(metrics, "Status", out _statusLabel);
        ShowcaseUi.AddMetricChip(metrics, "Room", out _roomLabel);

        _errorLabel = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Danger
        };
        cardBody.AddChild(_errorLabel);

        var actionRow = ShowcaseUi.AddActionRow(cardBody);
        _matchButton = ShowcaseUi.CreatePrimaryButton("Rematch");
        actionRow.AddChild(_matchButton);
    }
}
