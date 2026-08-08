using System.Threading;
using DotPudica.Core.Binding;

namespace DotPudica.Tests;

public class UiDispatcherTests
{
    [Fact]
    public void SynchronizationContextDispatcher_PostsOutsideCapturedContext()
    {
        var context = new QueuedSynchronizationContext();
        var dispatcher = UiDispatcher.FromSynchronizationContext(context);
        var executed = false;

        dispatcher.Post(() => executed = true);

        Assert.False(executed);
        context.RunAll();
        Assert.True(executed);
    }

    [Fact]
    public void SynchronizationContextDispatcher_RunsInlineOnCapturedContext()
    {
        var previousContext = SynchronizationContext.Current;
        var context = new QueuedSynchronizationContext();
        var dispatcher = UiDispatcher.FromSynchronizationContext(context);
        var executed = false;

        try
        {
            SynchronizationContext.SetSynchronizationContext(context);

            dispatcher.Post(() => executed = true);

            Assert.True(dispatcher.CheckAccess());
            Assert.True(executed);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    private sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _callbacks = new();

        public override void Post(SendOrPostCallback callback, object? state)
            => _callbacks.Enqueue((callback, state));

        public void RunAll()
        {
            while (_callbacks.Count > 0)
            {
                var (callback, state) = _callbacks.Dequeue();
                callback(state);
            }
        }
    }
}
