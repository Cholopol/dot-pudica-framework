namespace DotPudica.Core.ViewModels;

/// <summary>
/// Validatable ViewModel base class. Inherits from CommunityToolkit.Mvvm's ObservableValidator,
/// supports data annotation validation ([Required], [Range], [EmailAddress], etc.).
/// Corresponds to Loxodon's validatable ViewModel pattern.
/// </summary>
/// <example>
/// <code>
/// public partial class RegistrationViewModel : ValidatableViewModelBase
/// {
///     [ObservableProperty]
///     [Required(ErrorMessage = "Username cannot be empty")]
///     [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
///     string username = "";
///
///     [RelayCommand(CanExecute = nameof(ValidateAll))]
///     void Register() { ... }
/// }
/// </code>
/// </example>
public abstract class ValidatableViewModelBase : CommunityToolkit.Mvvm.ComponentModel.ObservableValidator, IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Validate all properties, returns whether all are valid.
    /// </summary>
    public bool ValidateAll()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    protected virtual void OnDispose() { }

    public void Dispose()
    {
        if (!_disposed)
        {
            CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
            OnDispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
