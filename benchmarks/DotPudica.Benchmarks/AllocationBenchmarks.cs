using BenchmarkDotNet.Attributes;
using DotPudica.Benchmarks.Fixtures;
using DotPudica.Core.Binding;

namespace DotPudica.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 8)]
public class AllocationBenchmarks
{
    private const int Burst = 1_000;

    private IntViewModel _typedVm = null!;
    private PropertyBinding<int, int> _typedBinding = null!;
    private ZeroAllocIntProxy _typedProxy = null!;

    private IntViewModel _equalVm = null!;
    private PropertyBinding<int, int> _equalBinding = null!;
    private ZeroAllocIntProxy _equalProxy = null!;

    private IntViewModel _objectVm = null!;
    private PropertyBinding _objectBinding = null!;
    private ObjectTargetProxy _objectProxy = null!;

    [GlobalSetup]
    public void Setup()
    {
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
        for (var i = 0; i < 100; i++)
            _typedVm.Value = i;

        _equalVm = new IntViewModel { Value = 42 };
        _equalProxy = new ZeroAllocIntProxy { Value = 42 };
        _equalBinding = new PropertyBinding<int, int>(
            _equalProxy,
            new TypedBindingPath<IntViewModel, int>(
                static vm => vm.Value,
                static (vm, v) => vm.Value = v,
                ["Value"]),
            BindingMode.OneWay);
        _equalBinding.Bind(_equalVm);

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
        for (var i = 0; i < 100; i++)
            _objectVm.Value = i;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _typedBinding.Dispose();
        _equalBinding.Dispose();
        _objectBinding.Dispose();
    }

    [Benchmark(Baseline = true)]
    public int TypedIntBurst()
    {
        var start = _typedVm.Value + 1;
        for (var i = 0; i < Burst; i++)
            _typedVm.Value = start + i;
        return _typedProxy.Value;
    }

    [Benchmark]
    public int TypedEqualSkipped()
    {
        for (var i = 0; i < Burst; i++)
            _equalVm.Value = 42;
        return _equalProxy.Value;
    }

    [Benchmark]
    public object? ObjectPipelineBurst()
    {
        var start = _objectVm.Value + 1;
        for (var i = 0; i < Burst; i++)
            _objectVm.Value = start + i;
        return _objectProxy.GetValue();
    }
}
