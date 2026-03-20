using DotPudica.Core.Logging;
using DotPudica.Core.Services;
using DotPudica.Godot.Logging;
using DotPudica.Godot.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DotPudica.Godot;

/// <summary>
/// DotPudica application context. Responsible for framework initialization, configuration and disposal.
/// Should be initialized in Godot's Autoload (singleton node) or the main scene's Node._Ready().
/// </summary>
/// <example>
/// Typical usage (in Autoload singleton node):
/// <code>
/// public partial class GameRoot : Node
/// {
///     private AppContext _app;
///
///     public override void _Ready()
///     {
///         _app = new AppContext().Initialize(services =>
///         {
///             services.AddSingleton&lt;IPlayerService, PlayerService&gt;();
///             services.AddTransient&lt;LoginViewModel&gt;();
///         });
///     }
///
///     public override void _ExitTree()
///     {
///         _app.Dispose();
///     }
/// }
/// </code>
/// </example>
public sealed class AppContext : IDisposable
{
    private static AppContext? _current;
    private GodotWindowManager? _windowManager;
    private bool _disposed;

    /// <summary>
    /// Current application context (global singleton).
    /// </summary>
    public static AppContext Current => _current
        ?? throw new InvalidOperationException("AppContext is not initialized, please call new AppContext().Initialize() first");

    /// <summary>
    /// Global window manager.
    /// </summary>
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
        if (_current != null)
            throw new InvalidOperationException("AppContext has already been initialized.");

        // 1. Initialize logging (switch to Godot backend)
        LogManager.Initialize(new GodotLogFactory());

        // 2. Configure IoC container
        ServiceLocator.Configure(services =>
        {
            // Register framework services
            services.AddSingleton(this);

            if (windowManagerNode != null)
                services.AddSingleton<IWindowManager>(windowManagerNode);

            // User custom registration
            configureServices?.Invoke(services);
        });

        // 3. Save window manager reference
        _windowManager = windowManagerNode;

        _current = this;
        return this;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _windowManager?.Clear();
            _current = null;
            _disposed = true;
        }
    }
}
