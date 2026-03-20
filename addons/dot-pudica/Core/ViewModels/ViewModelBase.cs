using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DotPudica.Core.Logging;

namespace DotPudica.Core.ViewModels;

/// <summary>
/// Base ViewModel class. Inherits from ObservableObject and integrates message bus and logging.
///
/// Developers' ViewModels should inherit directly from this class:
/// <code>
/// public partial class LoginViewModel : ViewModelBase
/// {
///     [ObservableProperty] string username = "";
///     [RelayCommand] void Login() { ... }
/// }
/// </code>
/// </summary>
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;

    private ILog? _log;

    /// <summary>
    /// Logger instance (lazy-loaded, named by type).
    /// </summary>
    protected ILog Log => _log ??= LogManager.GetLogger(GetType());

    /// <summary>
    /// Weak reference messenger (recommended to prevent memory leaks).
    /// </summary>
    protected IMessenger Messenger => WeakReferenceMessenger.Default;

    /// <summary>
    /// Short-cut method for sending messages.
    /// </summary>
    protected void Send<TMessage>(TMessage message) where TMessage : class
        => Messenger.Send(message);

    /// <summary>
    /// Subscribe to messages. Note: Call Messenger.UnregisterAll(this) to unsubscribe when the ViewModel is disposed.
    /// If using WeakReferenceMessenger (default), GC will automatically unbind it, no manual cancellation is required.
    /// </summary>
    protected void Register<TMessage>(MessageHandler<ViewModelBase, TMessage> handler)
        where TMessage : class
        => Messenger.Register(this, handler);

    /// <summary>
    /// Indicates whether the ViewModel has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Subclasses can override this method to perform custom cleanup logic.
    /// </summary>
    protected virtual void OnDispose() { }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Cancel all subscriptions of this ViewModel on the strong reference bus.
            // The weak reference bus is automatically handled by the GC; explicit cleanup here speeds up release.
            WeakReferenceMessenger.Default.UnregisterAll(this);
            OnDispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
