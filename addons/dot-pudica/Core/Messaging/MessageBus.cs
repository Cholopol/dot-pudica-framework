using CommunityToolkit.Mvvm.Messaging;

namespace DotPudica.Core.Messaging;

/// <summary>
/// Facade over CommunityToolkit Messenger. Prefer <see cref="Default"/> (weak) unless you need strong refs and will Unregister manually.
/// </summary>
public static class MessageBus
{
    public static IMessenger Default => WeakReferenceMessenger.Default;
    public static IMessenger Strong => StrongReferenceMessenger.Default;

    public static TMessage Send<TMessage>(TMessage message) where TMessage : class
        => WeakReferenceMessenger.Default.Send(message);

    public static TMessage Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
        => WeakReferenceMessenger.Default.Send(message, token);

    public static void Register<TRecipient, TMessage>(TRecipient recipient,
        MessageHandler<TRecipient, TMessage> handler)
        where TRecipient : class
        where TMessage : class
        => WeakReferenceMessenger.Default.Register(recipient, handler);

    public static void UnregisterAll(object recipient)
        => WeakReferenceMessenger.Default.UnregisterAll(recipient);

    public static void UnregisterAllStrong(object recipient)
        => StrongReferenceMessenger.Default.UnregisterAll(recipient);

    /// <summary>Clears handlers so ALC unload is not blocked.</summary>
    public static void Reset()
    {
        WeakReferenceMessenger.Default.Reset();
        StrongReferenceMessenger.Default.Reset();
    }
}
