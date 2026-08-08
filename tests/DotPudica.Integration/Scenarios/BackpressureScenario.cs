using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using Godot;
using Samples.Showcase.Shared.Probes;

namespace DotPudica.Integration.Scenarios;

/// <summary>Backpressure: 10000 Posts all execute in order; Mailbox only drains the latest (probe E).</summary>
public sealed class BackpressureScenario : IIntegrationScenario
{
    public string Name => "Backpressure_PostsCompleteAndMailboxCoalesces";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);

        var frameCounter = 0L;
        Callable bump = Callable.From(() => Interlocked.Increment(ref frameCounter));
        var tree = host.GetTree();
        tree.Connect(SceneTree.SignalName.ProcessFrame, bump);

        var probe = new BackpressureProbe();
        const int total = 10_000;

        try
        {
            probe.Reset("post-storm", total);
            var remaining = total;
            var done = new TaskCompletionSource();
            var lastSeq = -1;
            var orderBroken = false;

            await Task.Run(() =>
            {
                for (var i = 0; i < total; i++)
                {
                    var seq = i;
                    dispatcher.Post(() =>
                    {
                        if (seq != lastSeq + 1)
                            orderBroken = true;
                        lastSeq = seq;
                        probe.RecordExecuted(Interlocked.Read(ref frameCounter));
                        if (Interlocked.Decrement(ref remaining) == 0)
                            done.TrySetResult();
                    });
                }
            });

            await done.Task;
            await IntegrationTestHelpers.WaitFrames(host, 2);

            if (probe.TotalExecuted != total)
                return IntegrationResult.Fail(Name, $"Executed {probe.TotalExecuted} != {total}");
            if (orderBroken)
                return IntegrationResult.Fail(Name, "Post execution order was broken");

            // Mailbox: after many Publish calls, a single Drain only gets the latest
            var mailbox = new LatestSnapshotMailbox<int>();
            await Task.Run(() =>
            {
                for (var i = 1; i <= total; i++)
                    mailbox.Publish(i);
            });
            await IntegrationTestHelpers.WaitFrames(host, 1);
            if (!mailbox.TryDrainLatest(out var latest) || latest != total)
                return IntegrationResult.Fail(Name, $"Mailbox expected latest={total}, actual={latest}");
            if (mailbox.TryDrainLatest(out _))
                return IntegrationResult.Fail(Name, "Mailbox still has residual items after Drain");

            var evidence = probe.Evaluate();
            GD.Print($"[DotPudicaIntegration] EVIDENCE {Name}: {evidence.Observed}");
            return IntegrationResult.Pass(Name);
        }
        finally
        {
            if (tree.IsConnected(SceneTree.SignalName.ProcessFrame, bump))
                tree.Disconnect(SceneTree.SignalName.ProcessFrame, bump);
        }
    }
}
