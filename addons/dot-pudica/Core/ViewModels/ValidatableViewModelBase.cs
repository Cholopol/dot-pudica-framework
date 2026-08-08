using System.Diagnostics.CodeAnalysis;

namespace DotPudica.Core.ViewModels;

/// <summary>
/// ObservableValidator base. Dispose unregisters both messengers (ALC unload safety).
/// </summary>
public abstract class ValidatableViewModelBase : CommunityToolkit.Mvvm.ComponentModel.ObservableValidator, IDisposable
{
    private bool _disposed;

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "ObservableValidator.ValidateAllProperties is the supported CommunityToolkit entry point; " +
                        "partial ViewModels with [ObservableProperty] keep generated validator extensions via the MVVM source generator. " +
                        "Callers targeting full NativeAOT should prefer per-property validation if the linker strips the fast path.")]
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
            CommunityToolkit.Mvvm.Messaging.StrongReferenceMessenger.Default.UnregisterAll(this);
            OnDispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
