using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using DotPudica.Core.Binding;
using DotPudica.Core.Logging;
using DotPudica.Core.Messaging;
using DotPudica.Core.Services;

namespace DotPudica.Core.Runtime;

/// <summary>
/// Clears Core static roots when Godot unloads the collectible ALC
/// (play-start reload and play-stop unload; see godotengine/godot#78513).
/// </summary>
public static class FrameworkRuntime
{
    private static readonly object Gate = new();
    private static Action? _hostUnloadHandlers;
    private static bool _unloadHookRegistered;

    /// <summary>
    /// Extra host cleanup (e.g. Godot proxy factories). Runs during <see cref="Reset"/> and ALC <c>Unloading</c>.
    /// </summary>
    public static void RegisterHostUnloadHandler(Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (Gate)
        {
            EnsureUnloadHook();
            _hostUnloadHandlers += handler;
        }
    }

    /// <summary>
    /// Reset messengers, converter cache, logging, and the service locator.
    /// Idempotent; also invoked automatically on ALC unload.
    /// </summary>
    public static void Reset()
    {
        MessageBus.Reset();
        ConverterRegistry.Clear();
        LogManager.Reset();
        ServiceLocator.Reset();

        Action? hosts;
        lock (Gate)
            hosts = _hostUnloadHandlers;
        hosts?.Invoke();
    }

    [ModuleInitializer]
    [SuppressMessage(
        "Performance",
        "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Required so Godot's collectible ALC registers Unloading cleanup when the Core assembly loads.")]
    internal static void InitializeUnloadHook()
    {
        lock (Gate)
            EnsureUnloadHook();
    }

    private static void EnsureUnloadHook()
    {
        if (_unloadHookRegistered)
            return;

        var alc = AssemblyLoadContext.GetLoadContext(typeof(FrameworkRuntime).Assembly);
        alc!.Unloading += static _ => Reset();
        _unloadHookRegistered = true;
    }
}
