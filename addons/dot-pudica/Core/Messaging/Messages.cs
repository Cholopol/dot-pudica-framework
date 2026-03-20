namespace DotPudica.Core.Messaging;

/// <summary>
/// Message base class (optional). Message objects inheriting from this class can get automatic properties under Source Generator.
/// Can also directly use any class as message type, inheritance is not mandatory. This class is for convenience only (carries sender reference).
/// </summary>
public abstract class MessageBase
{
    /// <summary>Message sender (can be null).</summary>
    public object? Sender { get; init; }
}

/// <summary>
/// Message with strongly-typed value.
/// </summary>
public sealed class ValueChangedMessage<T> : MessageBase
{
    public T Value { get; }
    public ValueChangedMessage(T value, object? sender = null)
    {
        Value = value;
        Sender = sender;
    }
}

/// <summary>
/// Request-response message. Used for scenarios where sender needs receiver to return a value.
/// Based on CommunityToolkit.Mvvm.Messaging.Messages.RequestMessage
/// </summary>
public sealed class RequestMessage<TResponse> : CommunityToolkit.Mvvm.Messaging.Messages.RequestMessage<TResponse>
{
    public object? Sender { get; init; }
}

/// <summary>
/// Async request-response message.
/// </summary>
public sealed class AsyncRequestMessage<TResponse> : CommunityToolkit.Mvvm.Messaging.Messages.AsyncRequestMessage<TResponse>
{
    public object? Sender { get; init; }
}

/// <summary>
/// Notification message (no return value, carries string Key).
/// </summary>
public sealed class NotificationMessage : MessageBase
{
    public string Key { get; }
    public NotificationMessage(string key, object? sender = null)
    {
        Key = key;
        Sender = sender;
    }
}
