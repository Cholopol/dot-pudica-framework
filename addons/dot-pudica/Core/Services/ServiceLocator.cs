using System.Reflection;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DotPudica.Core.Services;

/// <summary>
/// Resettable root <see cref="IServiceProvider"/>; optionally mirrors into Toolkit <see cref="Ioc.Default"/>.
/// Caller owns the provider returned by <see cref="Configure"/> (or use <see cref="Reset"/>).
/// </summary>
public static class ServiceLocator
{
    private static readonly object Gate = new();
    private static IServiceProvider? _provider;

    public static ServiceProvider Configure(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var services = new ServiceCollection();
        configure(services);
        var provider = services.BuildServiceProvider();
        try
        {
            lock (Gate)
            {
                if (_provider is not null)
                    throw new InvalidOperationException(
                        "ServiceLocator has already been configured. Call Reset() before configuring again.");

                TryConfigureToolkitIoc(provider);
                _provider = provider;
            }

            return provider;
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }

    public static T Get<T>() where T : class
        => RequireProvider().GetRequiredService<T>();

    public static T? TryGet<T>() where T : class
        => RequireProvider().GetService<T>();

    public static IServiceProvider Provider => RequireProvider();

    public static void Reset()
    {
        IServiceProvider? provider;
        lock (Gate)
        {
            provider = _provider;
            _provider = null;
            ClearToolkitIoc();
        }

        if (provider is IDisposable disposable)
            disposable.Dispose();
    }

    private static IServiceProvider RequireProvider()
    {
        var provider = _provider;
        if (provider is null)
            throw new InvalidOperationException("ServiceLocator has not been configured yet.");
        return provider;
    }

    private static void TryConfigureToolkitIoc(IServiceProvider provider)
    {
        try
        {
            Ioc.Default.ConfigureServices(provider);
        }
        catch (InvalidOperationException)
        {
            // Toolkit Ioc has no public Reset; clear private field so hot reload can rebind.
            ClearToolkitIoc();
            Ioc.Default.ConfigureServices(provider);
        }
    }

    private static void ClearToolkitIoc()
    {
        var field = typeof(Ioc).GetField("serviceProvider", BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(Ioc.Default, null);
    }
}
