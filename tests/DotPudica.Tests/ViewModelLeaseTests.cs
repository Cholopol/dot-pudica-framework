using DotPudica.Core.ViewModels;

namespace DotPudica.Tests;

public class ViewModelLeaseTests
{
    [Fact]
    public void Owned_Dispose_DisposesViewModel()
    {
        var vm = new DisposableProbe();
        var lease = new ViewModelLease<DisposableProbe>(vm, ViewModelOwnership.Owned);
        lease.Dispose();
        Assert.True(vm.IsDisposed);
        Assert.Null(lease.ViewModel);
    }

    [Fact]
    public void External_Dispose_DoesNotDisposeViewModel()
    {
        var vm = new DisposableProbe();
        var lease = ViewModelLease<DisposableProbe>.External(vm);
        lease.Dispose();
        Assert.False(vm.IsDisposed);
        Assert.Null(lease.ViewModel);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var vm = new DisposableProbe();
        var lease = new ViewModelLease<DisposableProbe>(vm, ViewModelOwnership.Owned);
        lease.Dispose();
        lease.Dispose();
        Assert.Equal(1, vm.DisposeCount);
    }

    private sealed class DisposableProbe : IDisposable
    {
        public bool IsDisposed => DisposeCount > 0;
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }
}
