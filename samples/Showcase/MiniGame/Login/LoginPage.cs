using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;
using Samples.Showcase.MiniGame.Lobby;

namespace Samples.Showcase.MiniGame.Login;

/// <summary>MiniGame entry: login then open Lobby.</summary>
[DotPudicaView(typeof(LoginViewModel))]
public partial class LoginPage : ShowcasePageWindow
{
    [Export, BindTo(nameof(LoginViewModel.UserName), Mode = BindingMode.TwoWay)]
    private LineEdit _userNameInput = null!;

    [Export, BindTo(nameof(LoginViewModel.StatusText))]
    private Label _statusLabel = null!;

    [Export, BindCommand(nameof(LoginViewModel.LoginCommand))]
    private Button _loginButton = null!;

    public LoginPage()
    {
        WindowName = "Login";
    }

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    [Subscribe(nameof(LoginViewModel.LoginSucceeded))]
    private void OnLoginSucceeded()
    {
        var lobby = new LobbyPage { WindowName = "Lobby" };
        ShowFullPage(lobby);
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

        ShowcaseUi.AddSection(cardBody, "Login");
        ShowcaseUi.AddSubtitle(cardBody, "Sign in to enter the online match slice.");

        cardBody.AddChild(new Label { Text = "Username", Modulate = ShowcaseTheme.Muted });
        _userNameInput = new LineEdit { PlaceholderText = "At least 2 characters" };
        cardBody.AddChild(_userNameInput);

        _loginButton = ShowcaseUi.CreatePrimaryButton("Login");
        cardBody.AddChild(_loginButton);

        var metrics = ShowcaseUi.AddMetricsRow(cardBody);
        ShowcaseUi.AddMetricChip(metrics, "Status", out _statusLabel);
    }
}
