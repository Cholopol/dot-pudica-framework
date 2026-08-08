using DotPudica.Core.Messaging;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>Weak-reference Messenger: subscribers should not receive messages after being GC'd.</summary>
public sealed class MessagingLeakScenario : IIntegrationScenario
{
    public string Name => "Messaging_WeakSubscriptionDropsAfterGc";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var receivedAlive = 0;
        var receivedDead = 0;

        var alive = new Subscriber(() => Interlocked.Increment(ref receivedAlive));
        MessageBus.Register(alive, static (Subscriber s, PingMessage _) => s.Notify());

        WeakReference? weak;
        {
            var doomed = new Subscriber(() => Interlocked.Increment(ref receivedDead));
            MessageBus.Register(doomed, static (Subscriber s, PingMessage _) => s.Notify());
            weak = new WeakReference(doomed);
            doomed = null!;
        }

        // Force doomed to be collected
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await IntegrationTestHelpers.WaitFrames(host, 1);
        }

        if (weak.IsAlive)
        {
            // Some runtimes may delay collection; give it one more chance before asserting.
            await IntegrationTestHelpers.WaitFrames(host, 2);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        MessageBus.Send(new PingMessage());
        await IntegrationTestHelpers.WaitFrames(host, 1);

        try
        {
            if (receivedAlive != 1)
                return IntegrationResult.Fail(Name, $"Alive subscriber should have received 1, actual={receivedAlive}");

            if (weak.IsAlive)
            {
                return IntegrationResult.Fail(Name,
                    "Weak reference target was not GC'd, cannot verify 'no delivery after collection'; check if the scene still holds a strong reference.");
            }

            if (receivedDead != 0)
                return IntegrationResult.Fail(Name, $"Collected subscriber still received {receivedDead} messages");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            MessageBus.UnregisterAll(alive);
        }
    }

    private sealed class Subscriber(Action notify)
    {
        public void Notify() => notify();
    }

    private sealed class PingMessage;
}
