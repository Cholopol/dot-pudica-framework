namespace DotPudica.Core.Interactivity;

/// <summary>
/// ViewModel → View interaction without a View type reference.
/// No-op when nothing is subscribed to <see cref="Raised"/>.
/// </summary>
public sealed class InteractionRequest
{
    public event EventHandler? Raised;

    public void Raise() => Raised?.Invoke(this, EventArgs.Empty);
}
