using Godot;

namespace DotPudica.Integration.Benchmarks;

public partial class BenchmarkRunner : Node
{
    public override void _Ready() => CallDeferred(MethodName.RunAllDeferred);

    private async void RunAllDeferred()
    {
        var metrics = new BenchmarkMetricsCollector();
        var scenarios = new IBenchmarkScenario[]
        {
            new PropertyBurstMetricsScenario(),
            new NativeCompareMetricsScenario(),
            new BackpressureMetricsScenario(),
            new VirtualListMetricsScenario(),
            new ViewLifecycleMetricsScenario(),
            new PoolMetricsScenario(),
        };

        GD.Print(
            $"[DotPudicaBenchmark] START count={scenarios.Length} platform={OS.GetName()} " +
            $"mobile={OS.HasFeature("mobile")} editor={OS.HasFeature("editor")} userdata={OS.GetUserDataDir()}");
        var failed = 0;

        foreach (var scenario in scenarios)
        {
            try
            {
                GD.Print($"[DotPudicaBenchmark] RUN {scenario.Name}");
                await scenario.RunAsync(this, metrics).WaitAsync(TimeSpan.FromMinutes(5));
                GD.Print($"[DotPudicaBenchmark] PASS {scenario.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                GD.PrintErr($"[DotPudicaBenchmark] FAIL {scenario.Name}: {ex}");
            }
        }

        var wroteAny = false;
        foreach (var outputPath in ResolveMetricsOutputPaths())
        {
            try
            {
                metrics.Write(outputPath);
                wroteAny = true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DotPudicaBenchmark] WARN WriteMetrics {outputPath}: {ex.Message}");
            }
        }

        if (!wroteAny)
        {
            failed++;
            GD.PrintErr("[DotPudicaBenchmark] FAIL WriteMetrics: no writable output path");
        }

        var exitCode = failed == 0 ? 0 : 1;
        GD.Print($"[DotPudicaBenchmark] SUMMARY failed={failed} exit={exitCode}");
        GetTree().Quit(exitCode);
    }

    private static IEnumerable<string> ResolveMetricsOutputPaths()
    {
        // Export / mobile: user:// is always writable and retrievable from the device sandbox.
        yield return ProjectSettings.GlobalizePath("user://dotpudica-benchmarks/metrics.json");

        // Editor / desktop headless: also mirror into the repo tree for report scripts.
        if (!OS.HasFeature("mobile") && !OS.HasFeature("web"))
            yield return ProjectSettings.GlobalizePath("res://benchmarks/artifacts/godot/metrics.json");
    }
}
