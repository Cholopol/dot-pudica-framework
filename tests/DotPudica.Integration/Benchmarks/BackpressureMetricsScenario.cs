using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using Godot;
using Samples.Showcase.Shared.Probes;

namespace DotPudica.Integration.Benchmarks;

public sealed class BackpressureMetricsScenario : IBenchmarkScenario
{
    public string Name => "BackpressureMetrics";

    public async Task RunAsync(Node host, BenchmarkMetricsCollector metrics)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);

        var frameCounter = 0L;
        Callable bump = Callable.From(() => Interlocked.Increment(ref frameCounter));
        var tree = host.GetTree();
        tree.Connect(SceneTree.SignalName.ProcessFrame, bump);

        try
        {
            const int total = 10_000;
            var probe = new BackpressureProbe();

            probe.Reset("post-storm", total);
            var remaining = total;
            var done = new TaskCompletionSource();
            await Task.Run(() =>
            {
                for (var i = 0; i < total; i++)
                {
                    dispatcher.Post(() =>
                    {
                        probe.RecordExecuted(Interlocked.Read(ref frameCounter));
                        if (Interlocked.Decrement(ref remaining) == 0)
                            done.TrySetResult();
                    });
                }
            });
            await done.Task;
            await IntegrationTestHelpers.WaitFrames(host, 2);
            metrics.AddBackpressure(ToMetric(probe, total));

            metrics.AddBackpressure(await MeasureBudgetedAsync(host, probe, total, budget: 64, () => Interlocked.Read(ref frameCounter)));

            var mailbox = new LatestSnapshotMailbox<int>();
            probe.Reset("mailbox-drain", total);
            var flood = Task.Run(() =>
            {
                for (var i = 1; i <= total; i++)
                    mailbox.Publish(i);
            });

            var drained = 0;
            var guard = 0;
            while ((!flood.IsCompleted || drained == 0) && guard++ < 3_000)
            {
                await IntegrationTestHelpers.WaitFrames(host, 1);
                if (mailbox.TryDrainLatest(out _))
                {
                    drained++;
                    probe.RecordExecuted(Interlocked.Read(ref frameCounter));
                }
            }

            await IntegrationTestHelpers.WaitFrames(host, 2);
            if (mailbox.TryDrainLatest(out _))
            {
                drained++;
                probe.RecordExecuted(Interlocked.Read(ref frameCounter));
            }

            metrics.AddBackpressure(ToMetric(probe, total, drained));
        }
        finally
        {
            if (tree.IsConnected(SceneTree.SignalName.ProcessFrame, bump))
                tree.Disconnect(SceneTree.SignalName.ProcessFrame, bump);
        }
    }

    private static async Task<BackpressureMetric> MeasureBudgetedAsync(
        Node host,
        BackpressureProbe probe,
        int total,
        int budget,
        Func<long> readFrame)
    {
        probe.Reset("post-budgeted", total);
        var queue = new Queue<Action>();
        var gate = new object();

        await Task.Run(() =>
        {
            for (var i = 0; i < total; i++)
            {
                lock (gate)
                {
                    queue.Enqueue(() => probe.RecordExecuted(readFrame()));
                }
            }
        });

        var guard = 0;
        while (guard++ < 5_000)
        {
            var executedThisFrame = 0;
            while (executedThisFrame < budget)
            {
                Action? action = null;
                lock (gate)
                {
                    if (queue.Count > 0)
                        action = queue.Dequeue();
                }

                if (action is null)
                    break;

                action();
                executedThisFrame++;
            }

            int remaining;
            lock (gate)
                remaining = queue.Count;

            if (remaining == 0)
                break;

            await IntegrationTestHelpers.WaitFrames(host, 1);
        }

        await IntegrationTestHelpers.WaitFrames(host, 2);
        return new BackpressureMetric
        {
            Mode = "post-budgeted",
            Posted = total,
            Executed = probe.TotalExecuted,
            Drained = null,
            Budget = budget,
            FramesToComplete = probe.FramesToComplete,
            PeakPerFrame = probe.PerFrameCounts.Count == 0 ? 0 : probe.PerFrameCounts.Values.Max(),
            PerFrame = probe.PerFrameCounts
                .OrderBy(static kv => kv.Key)
                .Select(static kv => new BackpressureFrameMetric { Frame = kv.Key, Count = kv.Value })
                .ToList(),
        };
    }

    private static BackpressureMetric ToMetric(BackpressureProbe probe, int total, int? drained = null)
    {
        var peak = probe.PerFrameCounts.Count == 0 ? 0 : probe.PerFrameCounts.Values.Max();
        return new BackpressureMetric
        {
            Mode = probe.Mode,
            Posted = total,
            Executed = probe.TotalExecuted,
            Drained = drained,
            FramesToComplete = probe.FramesToComplete,
            PeakPerFrame = peak,
            PerFrame = probe.PerFrameCounts
                .OrderBy(static kv => kv.Key)
                .Select(static kv => new BackpressureFrameMetric { Frame = kv.Key, Count = kv.Value })
                .ToList(),
        };
    }
}
