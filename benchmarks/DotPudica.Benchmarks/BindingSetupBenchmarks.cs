using BenchmarkDotNet.Attributes;
using DotPudica.Benchmarks.Fixtures;
using DotPudica.Core.Binding;

namespace DotPudica.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 8)]
public class BindingSetupBenchmarks
{
    [Params(10, 50, 100)]
    public int BindCount { get; set; }

    [Benchmark(Baseline = true)]
    public int TypedBindAndDispose()
    {
        var vm = new IntViewModel { Value = 1 };
        var bindings = new PropertyBinding<int, int>[BindCount];
        for (var i = 0; i < BindCount; i++)
        {
            var proxy = new ZeroAllocIntProxy();
            bindings[i] = new PropertyBinding<int, int>(
                proxy,
                new TypedBindingPath<IntViewModel, int>(
                    static x => x.Value,
                    static (x, v) => x.Value = v,
                    ["Value"]),
                BindingMode.OneWay);
            bindings[i].Bind(vm);
        }

        for (var i = 0; i < BindCount; i++)
            bindings[i].Dispose();

        return BindCount;
    }

    [Benchmark]
    public int ObjectBindAndDispose()
    {
        var vm = new IntViewModel { Value = 1 };
        var bindings = new PropertyBinding[BindCount];
        for (var i = 0; i < BindCount; i++)
        {
            var proxy = new ObjectTargetProxy();
            bindings[i] = new PropertyBinding(
                proxy,
                new TypedBindingPath<IntViewModel, int>(
                    static x => x.Value,
                    static (x, v) => x.Value = v,
                    ["Value"]),
                BindingMode.OneWay);
            bindings[i].Bind(vm);
        }

        for (var i = 0; i < BindCount; i++)
            bindings[i].Dispose();

        return BindCount;
    }
}
