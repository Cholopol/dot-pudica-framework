using System.ComponentModel;
using DotPudica.Core.Binding;

namespace DotPudica.Tests;

/// <summary>
/// Allocation regression: verifies that value-type binding hot path does not allocate managed memory on property updates.
/// ViewModel uses cached PropertyChangedEventArgs, target proxy does not record history,
/// to avoid misattributing notification/test stub allocations as binding pipeline overhead.
/// </summary>
public class BindingAllocationRegressionTests
{
    [Fact]
    public void TypedIntBinding_PropertyUpdateBurst_AllocatesZero()
    {
        var viewModel = new ZeroAllocIntViewModel { Value = 0 };
        var proxy = new ZeroAllocIntProxy();
        var path = new TypedBindingPath<ZeroAllocIntViewModel, int>(
            static vm => vm.Value,
            static (vm, v) => vm.Value = v,
            ["Value"]);
        using var binding = new PropertyBinding<int, int>(proxy, path, BindingMode.OneWay);
        binding.Bind(viewModel);

        for (var i = 0; i < 100; i++)
            viewModel.Value = i;

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 100; i < 1_100; i++)
            viewModel.Value = i;
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
        Assert.Equal(1_099, proxy.Value);
    }

    [Fact]
    public void TypedIntBinding_EqualValueSkipped_AllocatesZero()
    {
        var viewModel = new ZeroAllocIntViewModel { Value = 42 };
        var proxy = new ZeroAllocIntProxy { Value = 42 };
        var path = new TypedBindingPath<ZeroAllocIntViewModel, int>(
            static vm => vm.Value,
            static (vm, v) => vm.Value = v,
            ["Value"]);
        using var binding = new PropertyBinding<int, int>(proxy, path, BindingMode.OneWay);
        binding.Bind(viewModel);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_000; i++)
            viewModel.Value = 42;
        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
    }

    private sealed class ZeroAllocIntViewModel : INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs ValueChangedArgs = new(nameof(Value));
        private int _value;

        public int Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;
                _value = value;
                PropertyChanged?.Invoke(this, ValueChangedArgs);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class ZeroAllocIntProxy : ITypedTargetProxy<int>
    {
        public int Value { get; set; }

        event EventHandler? ITypedTargetProxy<int>.ValueChanged
        {
            add { }
            remove { }
        }

        public int GetValue() => Value;
        public void SetValue(int value) => Value = value;
        public void Dispose() { }
    }
}
