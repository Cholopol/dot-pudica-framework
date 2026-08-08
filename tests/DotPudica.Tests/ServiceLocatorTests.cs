using DotPudica.Core.Runtime;
using DotPudica.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotPudica.Tests;

[Collection(FrameworkStaticCollection.Name)]
public class ServiceLocatorTests
{
    public ServiceLocatorTests()
    {
        FrameworkRuntime.Reset();
    }

    [Fact]
    public void Configure_ReturnedProvider_DisposesSingletons()
    {
        var provider = ServiceLocator.Configure(services => services.AddSingleton<DisposableService>());
        var service = ServiceLocator.Get<DisposableService>();

        provider.Dispose();

        Assert.True(service.Disposed);
        FrameworkRuntime.Reset();
    }

    [Fact]
    public void Configure_Reset_AllowsReconfigure()
    {
        ServiceLocator.Configure(services => services.AddSingleton<DisposableService>());
        FrameworkRuntime.Reset();

        var provider = ServiceLocator.Configure(services => services.AddSingleton<DisposableService>());
        Assert.NotNull(ServiceLocator.Get<DisposableService>());
        provider.Dispose();
        FrameworkRuntime.Reset();
    }

    private sealed class DisposableService : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}
