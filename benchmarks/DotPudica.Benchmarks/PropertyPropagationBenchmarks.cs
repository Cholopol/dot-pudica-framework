using BenchmarkDotNet.Attributes;
using DotPudica.Benchmarks.Fixtures;
using DotPudica.Core.Binding;

namespace DotPudica.Benchmarks;

/// <summary>Produce path: already-bound bindings applying N source updates.</summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 8)]
public class PropertyPropagationBenchmarks
{
    [Params(1000, 10_000)]
    public int N { get; set; }

    private IntViewModel _directVm = null!;
    private int _directSink;

    private IntViewModel _typedVm = null!;
    private PropertyBinding<int, int> _typedBinding = null!;
    private ZeroAllocIntProxy _typedProxy = null!;

    private IntViewModel _objectVm = null!;
    private PropertyBinding _objectBinding = null!;
    private ObjectTargetProxy _objectProxy = null!;

    [GlobalSetup]
    public void Setup()
    {
        _directVm = new IntViewModel { Value = 0 };

        _typedVm = new IntViewModel { Value = 0 };
        _typedProxy = new ZeroAllocIntProxy();
        _typedBinding = new PropertyBinding<int, int>(
            _typedProxy,
            new TypedBindingPath<IntViewModel, int>(
                static vm => vm.Value,
                static (vm, v) => vm.Value = v,
                ["Value"]),
            BindingMode.OneWay);
        _typedBinding.Bind(_typedVm);

        _objectVm = new IntViewModel { Value = 0 };
        _objectProxy = new ObjectTargetProxy();
        _objectBinding = new PropertyBinding(
            _objectProxy,
            new TypedBindingPath<IntViewModel, int>(
                static vm => vm.Value,
                static (vm, v) => vm.Value = v,
                ["Value"]),
            BindingMode.OneWay);
        _objectBinding.Bind(_objectVm);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _typedBinding.Dispose();
        _objectBinding.Dispose();
    }

    [Benchmark(Baseline = true)]
    public int DirectSetter()
    {
        for (var i = 0; i < N; i++)
        {
            _directVm.Value = i;
            _directSink = _directVm.Value;
        }

        return _directSink;
    }

    [Benchmark]
    public int TypedBinding()
    {
        for (var i = 0; i < N; i++)
            _typedVm.Value = i;
        return _typedProxy.Value;
    }

    [Benchmark]
    public object? ObjectBinding()
    {
        for (var i = 0; i < N; i++)
            _objectVm.Value = i;
        return _objectProxy.GetValue();
    }
}
