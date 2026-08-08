using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using DotPudica.Benchmarks;

var root = FindRepoRoot();
var artifactsDir = Path.Combine(root, "benchmarks", "artifacts", "core");
Directory.CreateDirectory(artifactsDir);

var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithArtifactsPath(artifactsDir)
    .AddExporter(JsonExporter.FullCompressed)
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

Console.WriteLine($"Running BenchmarkDotNet into {artifactsDir}");
_ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

var evidencePath = Path.Combine(artifactsDir, "evidence.json");
EvidenceCollector.Write(evidencePath);
Console.WriteLine($"Wrote evidence to {evidencePath}");

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "DotPudicaFramework.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }

    return Directory.GetCurrentDirectory();
}
