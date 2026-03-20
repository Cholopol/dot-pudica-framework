using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Binding.Converters;
using DotPudica.Core.Messaging;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.LoginScreen;

[DotPudicaView(typeof(LoginViewModel))]
public partial class LoginView : Control
{
    // TwoWay: user input -> ViewModel, ViewModel change -> Input box
    [Export, BindTo("Username", Mode = BindingMode.TwoWay)]
    private LineEdit _usernameInput = null!;

    [Export, BindTo("Password", Mode = BindingMode.TwoWay)]
    private LineEdit _passwordInput = null!;

    // OneWay: ViewModel updates Label when there are errors
    [Export, BindTo("ErrorMessage", Mode = BindingMode.OneWay)]
    private Label _errorLabel = null!;

    // OneWay + Value converter: Show progress bar when IsLoading=true, hide when false
    [Export, BindTo("IsLoading", Mode = BindingMode.OneWay,
        Converter = typeof(BoolToVisibilityConverter))]
    private ProgressBar _loadingBar = null!;

    // Command binding: button press -> LoginCommand.Execute()
    [Export, BindCommand("LoginCommand")]
    private Button _loginButton = null!;

    public override void _Ready()
    {
        ViewModel = new LoginViewModel();
        DotPudicaInitialize(); // Source Generator automatically completes all bindings here

        // Listen for login success global message
        DotPudica.Core.Messaging.MessageBus.Register<LoginView,
            DotPudica.Core.Messaging.NotificationMessage>(this, (view, msg) =>
        {
            if (msg.Key == "LoginSuccess")
            {
                GD.Print("[LoginView] Login successful! Redirecting to main screen...");
                // GetTree().ChangeSceneToFile("res://scenes/main.tscn");
            }
        });
    }

    public override void _ExitTree()
    {
        DotPudica.Core.Messaging.MessageBus.UnregisterAll(this);
        DotPudicaDispose();
        base._ExitTree();
    }
}
