using DotPudica.Core.Messaging;

namespace Samples.Showcase.Gallery.Messaging;

/// <summary>
/// Weak reference subscription demo object. The handler is registered as a <c>static</c> delegate to avoid closures pinning <c>this</c>.
/// The page side only keeps a <see cref="WeakReference{T}"/>; during GC probe, do not hold a strong reference obtained via <c>TryGetTarget</c>
/// in the same stack frame before <c>GC.Collect</c>, otherwise the object cannot be collected.
/// </summary>
internal sealed class GcProbeSubscriber
{
    public int ReceivedCount { get; private set; }

    public GcProbeSubscriber()
    {
        MessageBus.Register<GcProbeSubscriber, PingMessage>(this, static (recipient, _) =>
            recipient.ReceivedCount++);
    }
}
