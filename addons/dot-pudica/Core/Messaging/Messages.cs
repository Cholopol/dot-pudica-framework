namespace DotPudica.Core.Messaging;

/// <summary>Optional base with sender; any class can be a message without inheriting.</summary>
public abstract class MessageBase
{
    public object? Sender { get; init; }
}

public sealed class ValueChangedMessage<T> : MessageBase
{
    public T Value { get; }
    public ValueChangedMessage(T value, object? sender = null)
    {
        Value = value;
        Sender = sender;
    }
}

public sealed class RequestMessage<TResponse> : CommunityToolkit.Mvvm.Messaging.Messages.RequestMessage<TResponse>
{
    public object? Sender { get; init; }
}

public sealed class AsyncRequestMessage<TResponse> : CommunityToolkit.Mvvm.Messaging.Messages.AsyncRequestMessage<TResponse>
{
    public object? Sender { get; init; }
}

public sealed class NotificationMessage : MessageBase
{
    public string Key { get; }
    public NotificationMessage(string key, object? sender = null)
    {
        Key = key;
        Sender = sender;
    }
}
