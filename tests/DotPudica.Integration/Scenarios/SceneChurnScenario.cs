using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using Godot;
using Samples.Showcase.Shared.Probes;
using Samples.Showcase.Shared.Services;

namespace DotPudica.Integration.Scenarios;

/// <summary>Scene churn: repeatedly entering and exiting cancellation scopes, must not write back results after exit (probe F).</summary>
public sealed class SceneChurnScenario : IIntegrationScenario
{
    public string Name => "SceneChurn_CancelDropsResults";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);
        var probe = new CancelLifecycleProbe();
        probe.Reset();

        var matchService = new FakeShowcaseMatchService { Delay = TimeSpan.FromMilliseconds(500) };
        const int iterations = 40;
        var activeCts = 0;
        var tasks = new List<Task>(iterations);

        for (var i = 0; i < iterations; i++)
        {
            var scope = new SceneOperationScope();
            probe.OnEnter();
            var cts = scope.CreateLinkedTokenSource();
            Interlocked.Increment(ref activeCts);

            tasks.Add(RunOnceAsync(matchService, dispatcher, probe, cts, () =>
            {
                Interlocked.Decrement(ref activeCts);
            }));

            scope.Cancel();
            scope.Dispose();
            probe.OnExit();
        }

        await Task.WhenAll(tasks);
        await IntegrationTestHelpers.WaitFrames(host, 5);
        probe.ActiveCtsAtEnd = Volatile.Read(ref activeCts);

        var result = probe.Evaluate();
        return result.Verdict == ProbeVerdict.Pass
            ? IntegrationResult.Pass(Name)
            : IntegrationResult.Fail(Name, result.Observed);
    }

    private static async Task RunOnceAsync(
        IShowcaseMatchService service,
        IUiDispatcher dispatcher,
        CancelLifecycleProbe probe,
        CancellationTokenSource cts,
        Action onDone)
    {
        try
        {
            await service.MatchRoomAsync(cts.Token).ConfigureAwait(false);
            dispatcher.Post(probe.OnResultAfterExit);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            dispatcher.Post(probe.OnException);
        }
        finally
        {
            cts.Dispose();
            onDone();
        }
    }
}
