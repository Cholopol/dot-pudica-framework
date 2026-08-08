using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DotPudica.Core.Runtime;

namespace DotPudica.Godot;

/// <summary>
/// Registers Godot-host cleanup on the shared <see cref="FrameworkRuntime"/> unload path.
/// </summary>
internal static class GodotAssemblyUnloadHook
{
    [ModuleInitializer]
    [SuppressMessage(
        "Performance",
        "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Required so Godot host static caches clear on collectible ALC unload.")]
    internal static void Initialize()
    {
        FrameworkRuntime.RegisterHostUnloadHandler(static () =>
        {
            GodotTargetProxyFactory.Clear();
            AppContext.ResetForUnload();
        });
    }
}
