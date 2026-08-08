using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;
using DotPudica.Benchmarks.Fixtures;
using DotPudica.Core.Binding;

namespace DotPudica.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 8)]
public class CollectionBenchmarks
{
    [Params(1000, 10_000)]
    public int N { get; set; }

    private CollectionViewModel _vm = null!;
    private CollectionBinding _binding = null!;
    private StubItemsTargetProxy _proxy = null!;

    [IterationSetup]
    public void IterationSetup()
    {
        _vm = new CollectionViewModel();
        _proxy = new StubItemsTargetProxy();
        _binding = new CollectionBinding(
            _proxy,
            new TypedBindingPath<CollectionViewModel, ObservableCollection<string>>(
                static vm => vm.Items,
                static (vm, v) => { },
                ["Items"]));
        _binding.Bind(_vm);
    }

    [IterationCleanup]
    public void IterationCleanup() => _binding.Dispose();

    [Benchmark]
    public int AddItems()
    {
        for (var i = 0; i < N; i++)
            _vm.Items.Add($"item-{i}");
        return _proxy.Count;
    }
}
