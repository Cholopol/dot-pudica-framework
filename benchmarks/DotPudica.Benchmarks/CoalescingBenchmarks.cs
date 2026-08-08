using BenchmarkDotNet.Attributes;
using DotPudica.Benchmarks.Fixtures;
using DotPudica.Core.Binding;

namespace DotPudica.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 8)]
public class CoalescingBenchmarks
{
    [Params(1000, 10_000)]
    public int N { get; set; }

    private StringViewModel _uiVm = null!;
    private PropertyBinding<string, string> _uiBinding = null!;
    private CountingStringProxy _uiProxy = null!;

    private StringViewModel _bgVm = null!;
    private PropertyBinding<string, string> _bgBinding = null!;
    private CountingStringProxy _bgProxy = null!;
    private QueuedUiDispatcher _bgDispatcher = null!;

    [GlobalSetup]
    public void Setup()
    {
        _uiVm = new StringViewModel { Name = "initial" };
        _uiProxy = new CountingStringProxy();
        _uiBinding = new PropertyBinding<string, string>(
            _uiProxy,
            new TypedBindingPath<StringViewModel, string>(
                static vm => vm.Name,
                static (vm, v) => vm.Name = v,
                ["Name"]),
            BindingMode.OneWay);
        _uiBinding.Bind(_uiVm);

        _bgDispatcher = new QueuedUiDispatcher { HasAccess = true };
        _bgVm = new StringViewModel { Name = "initial" };
        _bgProxy = new CountingStringProxy();
        _bgBinding = new PropertyBinding<string, string>(
            _bgProxy,
            new TypedBindingPath<StringViewModel, string>(
                static vm => vm.Name,
                static (vm, v) => vm.Name = v,
                ["Name"]),
            BindingMode.OneWay,
            dispatcher: _bgDispatcher);
        _bgBinding.Bind(_bgVm);
        _bgDispatcher.RunAll();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _uiBinding.Dispose();
        _bgBinding.Dispose();
    }

    [Benchmark(Baseline = true)]
    public int UiThreadBurst()
    {
        var before = _uiProxy.SetValueCallCount;
        for (var i = 0; i < N; i++)
            _uiVm.Name = $"value-{i}";
        return _uiProxy.SetValueCallCount - before;
    }

    [Benchmark]
    public int BackgroundBurstThenDrain()
    {
        _bgDispatcher.HasAccess = false;
        var before = _bgProxy.SetValueCallCount;
        for (var i = 0; i < N; i++)
            _bgVm.Name = $"value-{i}";
        var pending = _bgDispatcher.PendingCount;
        _bgDispatcher.HasAccess = true;
        _bgDispatcher.RunAll();
        var writes = _bgProxy.SetValueCallCount - before;
        return pending * 1_000_000 + writes;
    }
}
