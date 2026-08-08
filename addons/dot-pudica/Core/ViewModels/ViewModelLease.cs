namespace DotPudica.Core.ViewModels;

/// <summary>
/// External: unbind only. Owned: also Dispose the ViewModel (if <see cref="IDisposable"/>) on release.
/// </summary>
public enum ViewModelOwnership
{
    External,
    Owned
}

/// <summary>Pairs a ViewModel with ownership for Runtime teardown.</summary>
internal sealed class ViewModelLease<TViewModel> : IDisposable
    where TViewModel : class
{
    private bool _disposed;

    public ViewModelLease(TViewModel? viewModel, ViewModelOwnership ownership)
    {
        ViewModel = viewModel;
        Ownership = ownership;
    }

    public TViewModel? ViewModel { get; private set; }

    public ViewModelOwnership Ownership { get; }

    public static ViewModelLease<TViewModel> External(TViewModel? viewModel)
        => new(viewModel, ViewModelOwnership.External);

    public void Dispose()
    {
        if (_disposed)
            return;

        if (Ownership == ViewModelOwnership.Owned && ViewModel is IDisposable disposable)
            disposable.Dispose();

        ViewModel = null;
        _disposed = true;
    }
}
