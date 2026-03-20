using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DotPudica.Core.Services;

/// <summary>
/// Global service locator. Wraps CommunityToolkit.Mvvm's Ioc.Default,
/// provides a unified entry point for registering and resolving services.
/// </summary>
/// <example>
/// Initialization (configure once at game startup):
/// <code>
/// ServiceLocator.Configure(services =>
/// {
///     services.AddSingleton&lt;IGameService, GameService&gt;();
///     services.AddTransient&lt;LoginViewModel&gt;();
/// });
/// </code>
/// Usage:
/// <code>
/// var service = ServiceLocator.Get&lt;IGameService&gt;();
/// </code>
/// </example>
public static class ServiceLocator
{
    /// <summary>
    /// Configure and initialize the IoC container. Should only be called once at application startup.
    /// </summary>
    public static void Configure(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        Ioc.Default.ConfigureServices(services.BuildServiceProvider());
    }

    /// <summary>
    /// Resolve registered services (singleton, transient, etc.).
    /// </summary>
    public static T Get<T>() where T : class
        => Ioc.Default.GetRequiredService<T>();

    /// <summary>
    /// Try to resolve service, returns null if not registered.
    /// </summary>
    public static T? TryGet<T>() where T : class
        => Ioc.Default.GetService<T>();

    /// <summary>
    /// Get the underlying IServiceProvider, for advanced scenarios.
    /// </summary>
    public static IServiceProvider Provider => Ioc.Default;
}
