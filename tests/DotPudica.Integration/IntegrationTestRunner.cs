using Godot;

namespace DotPudica.Integration;

/// <summary>
/// Godot headless integration test entry point. Executes scenarios sequentially and reports results via process exit code.
/// </summary>
public partial class IntegrationTestRunner : Node
{
    public override void _Ready() => CallDeferred(MethodName.RunAllDeferred);

    private async void RunAllDeferred()
    {
        var scenarios = new IIntegrationScenario[]
        {
            new Scenarios.DispatcherScenario(),
            new Scenarios.PropertyBurstScenario(),
            new Scenarios.CollectionScenario(),
            new Scenarios.LifecycleScenario(),
            new Scenarios.StaleWorkScenario(),
            new Scenarios.VirtualListScenario(),
            new Scenarios.MatchCancelScenario(),
            new Scenarios.SnapshotMailboxScenario(),
            new Scenarios.SharedInventoryScenario(),
            new Scenarios.DispatchOrderScenario(),
            new Scenarios.CollectionMainThreadScenario(),
            new Scenarios.ItemsSourceContainerScenario(),
            new Scenarios.DeclarativeItemsSourceScenario(),
            new Scenarios.ContractViolationScenario(),
            new Scenarios.BackpressureScenario(),
            new Scenarios.SceneChurnScenario(),
            new Scenarios.WindowStackScenario(),
            new Scenarios.WindowDismissReentrantScenario(),
            new Scenarios.WindowDismissThenShowScenario(),
            new Scenarios.WindowFadeContractScenario(),
            new Scenarios.MessagingLeakScenario(),
            new Scenarios.DeclarativeBindingScenario(),
            new Scenarios.RangeWriteOrderScenario(),
            new Scenarios.WindowPoolScenario(),
            new Scenarios.ViewPoolScenario(),
            new Scenarios.ViewPoolAutoInitScenario(),
        };

        var failed = 0;
        GD.Print($"[DotPudicaIntegration] START count={scenarios.Length}");

        foreach (var scenario in scenarios)
        {
            IntegrationResult result;
            try
            {
                result = await scenario.RunAsync(this).WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                result = IntegrationResult.Fail(scenario.Name, ex.ToString());
            }

            if (result.Passed)
            {
                GD.Print($"[DotPudicaIntegration] PASS {result.Name}");
            }
            else
            {
                failed++;
                GD.PrintErr($"[DotPudicaIntegration] FAIL {result.Name}: {result.FailureReason}");
            }
        }

        var exitCode = failed == 0 ? 0 : 1;
        GD.Print($"[DotPudicaIntegration] SUMMARY passed={scenarios.Length - failed} failed={failed} exit={exitCode}");
        GetTree().Quit(exitCode);
    }
}
