using CommunityToolkit.Mvvm.Messaging;

namespace DotPudica.Core.Messaging;

/// <summary>
/// Message bus static facade. Wraps CommunityToolkit.Mvvm's Messenger,
/// provides convenient global message sending/receiving, supports both weak reference (auto-unbind) and strong reference modes.
/// </summary>
public static class MessageBus
{
    /// <summary>
    /// Default weak reference message bus (recommended, prevents memory leaks).
    /// </summary>
    public static IMessenger Default => WeakReferenceMessenger.Default;

    /// <summary>
    /// Strong reference message bus (slightly better performance, but must manually Unregister).
    /// </summary>
    public static IMessenger Strong => StrongReferenceMessenger.Default;

    // === Weak reference version shortcut methods ===

    /// <summary>
    /// Send message (weak reference bus).
    /// </summary>
    public static TMessage Send<TMessage>(TMessage message) where TMessage : class
        => WeakReferenceMessenger.Default.Send(message);

    /// <summary>
    /// Send message with channel key (weak reference bus). Suitable for scenarios where the same message type needs to be isolated in different logical channels.
    /// </summary>
    public static TMessage Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
        => WeakReferenceMessenger.Default.Send(message, token);

    /// <summary>
    /// Subscribe to message (weak reference bus).
    /// </summary>
    public static void Register<TRecipient, TMessage>(TRecipient recipient,
        MessageHandler<TRecipient, TMessage> handler)
        where TRecipient : class
        where TMessage : class
        => WeakReferenceMessenger.Default.Register(recipient, handler);

    /// <summary>
    /// Unsubscribe all messages for the specified recipient (weak reference bus).
    /// </summary>
    public static void UnregisterAll(object recipient)
        => WeakReferenceMessenger.Default.UnregisterAll(recipient);
}
