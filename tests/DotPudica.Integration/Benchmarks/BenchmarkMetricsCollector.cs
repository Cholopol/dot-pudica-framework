using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace DotPudica.Integration.Benchmarks;

public sealed class BenchmarkMetricsCollector
{
    private readonly List<PropertyBurstMetric> _propertyBurst = new();
    private readonly List<BackpressureMetric> _backpressure = new();
    private readonly List<VirtualListMetric> _virtualList = new();
    private readonly List<ViewLifecycleMetric> _viewLifecycle = new();
    private readonly List<NativeCompareMetric> _nativeCompare = new();
    private readonly List<PoolMetric> _pool = new();

    public void AddPropertyBurst(PropertyBurstMetric metric) => _propertyBurst.Add(metric);
    public void AddBackpressure(BackpressureMetric metric) => _backpressure.Add(metric);
    public void AddVirtualList(VirtualListMetric metric) => _virtualList.Add(metric);
    public void AddViewLifecycle(ViewLifecycleMetric metric) => _viewLifecycle.Add(metric);
    public void AddNativeCompare(NativeCompareMetric metric) => _nativeCompare.Add(metric);
    public void AddPool(PoolMetric metric) => _pool.Add(metric);

    public void Write(string outputPath)
    {
        var payload = new BenchmarkMetricsDocument
        {
            Kind = "godot-metrics",
            CapturedAtUtc = DateTime.UtcNow.ToString("O"),
            PropertyBurst = _propertyBurst,
            Backpressure = _backpressure,
            VirtualList = _virtualList,
            ViewLifecycle = _viewLifecycle,
            NativeCompare = _nativeCompare,
            Pool = _pool,
        };

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // NativeAOT disables reflection-based STJ; use source-generated contract.
        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(payload, BenchmarkMetricsJsonContext.Default.BenchmarkMetricsDocument));
        GD.Print($"[DotPudicaBenchmark] Wrote metrics to {outputPath}");
    }
}

public sealed class BenchmarkMetricsDocument
{
    public string Kind { get; set; } = "godot-metrics";
    public string CapturedAtUtc { get; set; } = "";
    public List<PropertyBurstMetric> PropertyBurst { get; set; } = new();
    public List<BackpressureMetric> Backpressure { get; set; } = new();
    public List<VirtualListMetric> VirtualList { get; set; } = new();
    public List<ViewLifecycleMetric> ViewLifecycle { get; set; } = new();
    public List<NativeCompareMetric> NativeCompare { get; set; } = new();
    public List<PoolMetric> Pool { get; set; } = new();
}

public sealed class PropertyBurstMetric
{
    public int SourceUpdates { get; set; }
    public int TargetWrites { get; set; }
    public int FramesToSettle { get; set; }
    public double ElapsedMs { get; set; }
    public string FinalValue { get; set; } = "";
    public bool Settled { get; set; }
}

public sealed class BackpressureFrameMetric
{
    public long Frame { get; set; }
    public int Count { get; set; }
}

public sealed class BackpressureMetric
{
    public string Mode { get; set; } = "";
    public int Posted { get; set; }
    public int Executed { get; set; }
    public int? Drained { get; set; }
    public int? Budget { get; set; }
    public long FramesToComplete { get; set; }
    public int PeakPerFrame { get; set; }
    public List<BackpressureFrameMetric> PerFrame { get; set; } = new();
}

public sealed class VirtualListMetric
{
    public string Mode { get; set; } = "";
    public int ItemCount { get; set; }
    public int ActiveNodes { get; set; }
    public double BindMs { get; set; }
    public double ScrollMs { get; set; }
}

public sealed class ViewLifecycleMetric
{
    public int BindCount { get; set; }
    public double InitMs { get; set; }
    public double DisposeMs { get; set; }
}

public sealed class NativeCompareMetric
{
    public string Mode { get; set; } = "";
    public int SourceUpdates { get; set; }
    public int TargetWrites { get; set; }
    public int FramesToSettle { get; set; }
    public double ElapsedMs { get; set; }
    public string FinalValue { get; set; } = "";
    public bool Settled { get; set; }
}

public sealed class PoolMetric
{
    public string Mode { get; set; } = "";
    public int Iterations { get; set; }
    public int CreatedNodes { get; set; }
    public int ReusedCount { get; set; }
    public double ElapsedMs { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BenchmarkMetricsDocument))]
internal partial class BenchmarkMetricsJsonContext : JsonSerializerContext
{
}

public interface IBenchmarkScenario
{
    string Name { get; }
    Task RunAsync(Node host, BenchmarkMetricsCollector metrics);
}
