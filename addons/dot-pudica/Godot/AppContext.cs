using DotPudica.Core.Logging;
using DotPudica.Core.Runtime;
using DotPudica.Core.Services;
using DotPudica.Godot.Logging;
using DotPudica.Godot.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DotPudica.Godot;

/// <summary>
/// DotPudica application context. Responsible for framework initialization, configuration and disposal.
/// Should be initialized in Godot's Autoload (singleton node) or the main scene's Node._Ready().
/// </summary>
public sealed class AppContext : IDisposable
{
    private static AppContext? _current;
    private static AppContextLifecycle _lifecycle;
    private GodotWindowManager? _windowManager;
    private ServiceProvider? _serviceProvider;

    public static AppContext Current => _current
        ?? throw new InvalidOperationException("AppContext is not initialized, please call new AppContext().Initialize() first");

    /// <summary>
    /// Root <see cref="IServiceProvider"/> for the process lifetime. Scene scopes should be created from this provider.
    /// </summary>
    public IServiceProvider Services => _serviceProvider
        ?? throw new InvalidOperationException("AppContext service provider is not available.");

    public GodotWindowManager WindowManager => _windowManager
        ?? throw new InvalidOperationException("WindowManager is not configured, please set windowManagerNode in Initialize");

    /// <summary>
    /// Initialize framework: configure logging, IoC container, window manager.
    /// </summary>
    /// <param name="configureServices">Service registration callback</param>
    /// <param name="windowManagerNode">GodotWindowManager node in scene tree (optional)</param>
    public AppContext Initialize(
        Action<IServiceCollection>? configureServices = null,
        GodotWindowManager? windowManagerNode = null)
    {
        if (_lifecycle is not AppContextLifecycle.Uninitialized)
            throw new InvalidOperationException("AppContext can only be initialized once until disposed or ALC unload resets it.");

        LogManager.Initialize(new GodotLogFactory());

        _serviceProvider = ServiceLocator.Configure(services =>
        {
            services.AddSingleton(this);

            if (windowManagerNode != null)
                services.AddSingleton<IWindowManager>(windowManagerNode);

            configureServices?.Invoke(services);
        });

        _windowManager = windowManagerNode;

        _current = this;
        _lifecycle = AppContextLifecycle.Running;
        return this;
    }

    public void Dispose()
    {
        if (_lifecycle is not AppContextLifecycle.Running || !ReferenceEquals(_current, this))
            return;

        _lifecycle = AppContextLifecycle.Disposing;
        try
        {
            _windowManager?.Clear();
        }
        finally
        {
            _windowManager = null;
            // ServiceLocator.Reset (via FrameworkRuntime) owns provider disposal.
            _serviceProvider = null;
            _current = null;
            _lifecycle = AppContextLifecycle.Uninitialized;
            FrameworkRuntime.Reset();
        }
    }

    /// <summary>
    /// Clears static AppContext state during ALC unload without disposing Godot nodes.
    /// </summary>
    internal static void ResetForUnload()
    {
        var current = _current;
        if (current is not null)
        {
            current._windowManager = null;
            current._serviceProvider = null;
        }

        _current = null;
        _lifecycle = AppContextLifecycle.Uninitialized;
    }

    private enum AppContextLifecycle
    {
        Uninitialized,
        Running,
        Disposing
    }
}
