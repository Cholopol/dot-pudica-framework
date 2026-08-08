using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Validation;

/// <summary>
/// Validation demo: ValidatableViewModelBase + DataAnnotations,
/// HasErrors / GetErrors and CanExecute-gated submit.
/// </summary>
public partial class ValidationViewModel : ValidatableViewModelBase
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    private string _username = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    private string _email = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
    private int _age = 18;

    [ObservableProperty]
    private string _summaryText = "Not submitted yet.";

    [ObservableProperty]
    private string _errorSummary = "No errors";

    /// <summary>
    /// Exposes base ObservableValidator.HasErrors for [BindTo] path resolution.
    /// </summary>
    public new bool HasErrors => base.HasErrors;

    public ValidationViewModel()
    {
        ValidateAllProperties();
        RefreshErrorSummary();
    }

    [RelayCommand(CanExecute = nameof(ValidateAll))]
    private void Submit()
    {
        SummaryText = $"Submitted: {Username} / {Email} / age {Age}";
    }

    [RelayCommand]
    private void ValidateAllNow()
    {
        var isValid = ValidateAll();
        SummaryText = isValid ? "All fields valid." : "Fix errors below.";
        RefreshErrorSummary();
        SubmitCommand.NotifyCanExecuteChanged();
    }

    partial void OnUsernameChanged(string value) => OnValidatedPropertyChanged();

    partial void OnEmailChanged(string value) => OnValidatedPropertyChanged();

    partial void OnAgeChanged(int value) => OnValidatedPropertyChanged();

    private void OnValidatedPropertyChanged()
    {
        SubmitCommand.NotifyCanExecuteChanged();
        RefreshErrorSummary();
    }

    private void RefreshErrorSummary()
    {
        if (!HasErrors)
        {
            ErrorSummary = "No errors";
            return;
        }

        var lines = GetErrors(null)
            .Cast<ValidationResult>()
            .Select(error => error.ErrorMessage ?? "Unknown error");
        ErrorSummary = string.Join("\n", lines);
    }
}
