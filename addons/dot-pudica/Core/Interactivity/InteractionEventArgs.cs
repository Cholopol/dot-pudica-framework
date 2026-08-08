namespace DotPudica.Core.Interactivity;

public sealed class InteractionEventArgs<T> : EventArgs
{
    public InteractionEventArgs(T context, Action? callback)
    {
        Context = context;
        Callback = callback;
    }

    public T Context { get; }

    /// <summary>
    /// Invoked after the View finishes; wraps the VM-side <c>Action&lt;T&gt;</c>. Null when none.
    /// </summary>
    public Action? Callback { get; }
}
