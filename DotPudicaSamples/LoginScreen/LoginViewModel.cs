
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

namespace Samples.LoginScreen;

/// <summary>
/// Login screen ViewModel. Demonstrates async commands, waiting states, and error prompts.
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    // CanExecute: button is clickable only when both username and password are not empty
    private bool CanLogin()
        => !string.IsNullOrWhiteSpace(Username)
           && !string.IsNullOrWhiteSpace(Password)
           && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = "";

        // Simulate network request waiting for 1.5 seconds
        await Task.Delay(1500);

        // Simulate verification logic
        if (Username == "admin" && Password == "123456")
        {
            // Login successful, broadcast globally
            Send(new DotPudica.Core.Messaging.NotificationMessage("LoginSuccess"));
        }
        else
        {
            ErrorMessage = "Username or password is incorrect, please try again.";
        }

        IsLoading = false;
    }
}
