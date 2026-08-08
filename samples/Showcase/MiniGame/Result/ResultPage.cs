using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Samples.Showcase.MiniGame.Lobby;
using Samples.Showcase.MiniGame.Login;
using Samples.Showcase.Shared.Models;
using Samples.Showcase.Shared.Services;
using AppContext = DotPudica.Godot.AppContext;

namespace Samples.Showcase.MiniGame.Result;

/// <summary>Result page after battle.</summary>
[DotPudicaView(typeof(ResultViewModel))]
public partial class ResultPage : ShowcasePageWindow
{
    [Export, BindTo(nameof(ResultViewModel.TitleText))]
    private Label _titleLabel = null!;

    [Export, BindTo(nameof(ResultViewModel.ScoreText))]
    private Label _scoreLabel = null!;

    [Export, BindTo(nameof(ResultViewModel.DurationText))]
    private Label _durationLabel = null!;

    [Export, BindTo(nameof(ResultViewModel.LoadoutText))]
    private Label _loadoutLabel = null!;

    [Export, BindCommand(nameof(ResultViewModel.BackToLobbyCommand))]
    private Button _backToLobbyButton = null!;

    [Export, BindCommand(nameof(ResultViewModel.BackToLoginCommand))]
    private Button _backToLoginButton = null!;

    private BattleResult _battleResult = new(0, false, TimeSpan.Zero);

    public ResultPage()
    {
        WindowName = "Result";
    }

    public void SetBattleResult(BattleResult result) => _battleResult = result;

    [ViewModelFactory]
    private ResultViewModel CreateResultViewModel() => new(_battleResult);

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady()
    {
        EnsureControls();
        _titleLabel.Modulate = _battleResult.Won ? ShowcaseTheme.Success : ShowcaseTheme.Danger;
    }

    [Subscribe(nameof(ResultViewModel.BackToLobbyRequested))]
    private void OnBackToLobbyRequested()
    {
        var lobby = new LobbyPage { WindowName = "Lobby" };
        ShowFullPage(lobby);
        RequireWindowManager().Dismiss(this);
    }

    [Subscribe(nameof(ResultViewModel.BackToLoginRequested))]
    private void OnBackToLoginRequested()
    {
        AppContext.Current.Services.GetRequiredService<IProfileService>().Logout();
        var login = new LoginPage { WindowName = "Login" };
        ShowFullPage(login);
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

        var cardBody = ShowcaseUi.CreateCardBody(out var panel, pad: 20);
        panel.CustomMinimumSize = new Vector2(520, 0);
        center.AddChild(panel);

        _titleLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 28);
        cardBody.AddChild(_titleLabel);

        var metrics = ShowcaseUi.AddMetricsRow(cardBody);
        ShowcaseUi.AddMetricChip(metrics, "Score", out _scoreLabel, expand: true);
        ShowcaseUi.AddMetricChip(metrics, "Duration", out _durationLabel, expand: true);

        ShowcaseUi.AddSection(cardBody, "Loadout");
        _loadoutLabel = new Label
        {
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = ShowcaseTheme.Text
        };
        cardBody.AddChild(_loadoutLabel);

        var actionRow = ShowcaseUi.AddActionRow(cardBody);
        actionRow.Alignment = BoxContainer.AlignmentMode.Center;
        _backToLobbyButton = ShowcaseUi.CreatePrimaryButton("Back to Lobby");
        _backToLoginButton = ShowcaseUi.CreateActionButton("Back to Login");
        actionRow.AddChild(_backToLobbyButton);
        actionRow.AddChild(_backToLoginButton);
    }
}
