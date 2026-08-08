namespace DotPudica.Core.Interactivity;

/// <summary>
/// Typed-context interaction. Optional completion callback lets the View notify the VM
/// without calling VM methods directly. No-op when nothing is subscribed.
/// </summary>
public sealed class InteractionRequest<T>
{
    public event EventHandler<InteractionEventArgs<T>>? Raised;

    public void Raise(T context) => Raise(context, null);

    /// <summary>
    /// View should invoke <see cref="InteractionEventArgs{T}.Callback"/> after completion
    /// so the <paramref name="callback"/> closing over <paramref name="context"/> runs.
    /// </summary>
    public void Raise(T context, Action<T>? callback)
    {
        var handler = Raised;
        if (handler is null)
            return;

        Action? wrapped = callback is null ? null : () => callback(context);
        handler(this, new InteractionEventArgs<T>(context, wrapped));
    }
}
