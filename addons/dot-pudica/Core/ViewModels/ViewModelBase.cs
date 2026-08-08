using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DotPudica.Core.Logging;

namespace DotPudica.Core.ViewModels;

/// <summary>
/// ObservableObject + weak messenger + logging. Dispose unregisters both messengers (ALC unload safety).
/// </summary>
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;
    private ILog? _log;

    protected ILog Log => _log ??= LogManager.GetLogger(GetType());
    protected IMessenger Messenger => WeakReferenceMessenger.Default;

    protected void Send<TMessage>(TMessage message) where TMessage : class
        => Messenger.Send(message);

    protected void Register<TMessage>(MessageHandler<ViewModelBase, TMessage> handler)
        where TMessage : class
        => Messenger.Register(this, handler);

    public bool IsDisposed => _disposed;

    protected virtual void OnDispose() { }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Explicit unregister so ALC unload is not blocked by leftover handlers.
            WeakReferenceMessenger.Default.UnregisterAll(this);
            StrongReferenceMessenger.Default.UnregisterAll(this);
            OnDispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
