using DotPudica.Core.Binding;
using DotPudica.Godot.Views;
using Godot;
using Samples.Showcase.Shared.Probes;

namespace DotPudica.Integration.Scenarios;

/// <summary>Direct IUiDispatcher.Post verification: FIFO order and full execution (probe D).</summary>
public sealed class DispatchOrderScenario : IIntegrationScenario
{
    public string Name => "DispatchOrder_IsFifoAndComplete";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);
        var probe = new DispatchOrderProbe();
        probe.Reset();

        var frameCounter = 0L;
        void OnFrame() => Interlocked.Increment(ref frameCounter);
        // Use ProcessFrame signal to approximate frame count
        var tree = host.GetTree();
        Callable bump = Callable.From(OnFrame);
        tree.Connect(SceneTree.SignalName.ProcessFrame, bump);

        const int n = 200;
        try
        {
            await Task.Run(() =>
            {
                for (var i = 1; i <= n; i++)
                {
                    var seq = i;
                    var postFrame = Interlocked.Read(ref frameCounter);
                    probe.RecordPosted();
                    dispatcher.Post(() =>
                    {
                        var execFrame = Interlocked.Read(ref frameCounter);
                        probe.RecordExecuted(seq, postFrame, execFrame);
                    });
                }
            });
            await IntegrationTestHelpers.WaitFrames(host, 15);

            var result = probe.Evaluate();
            return result.Verdict == ProbeVerdict.Pass
                ? IntegrationResult.Pass(Name)
                : IntegrationResult.Fail(Name, result.Observed);
        }
        finally
        {
            if (tree.IsConnected(SceneTree.SignalName.ProcessFrame, bump))
                tree.Disconnect(SceneTree.SignalName.ProcessFrame, bump);
        }
    }
}
