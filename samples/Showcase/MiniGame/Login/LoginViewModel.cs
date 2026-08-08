using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;
using Samples.Showcase.Shared.Services;

namespace Samples.Showcase.MiniGame.Login;

/// <summary>Login ViewModel with DataAnnotations validation.</summary>
public partial class LoginViewModel : ValidatableViewModelBase
{
    private readonly IProfileService _profileService;

    public LoginViewModel(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Username is required")]
    [MinLength(2, ErrorMessage = "Username must be at least 2 characters")]
    private string _userName = "";

    [ObservableProperty]
    private string _statusText = "Enter a username to continue";

    public event Action? LoginSucceeded;

    [RelayCommand]
    private void Login()
    {
        if (!ValidateAll())
        {
            var errors = GetErrors(nameof(UserName))
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrEmpty(m));
            StatusText = string.Join("; ", errors);
            return;
        }

        _profileService.Login(UserName);
        StatusText = $"Signed in: {_profileService.CurrentUserName}";
        LoginSucceeded?.Invoke();
    }
}
