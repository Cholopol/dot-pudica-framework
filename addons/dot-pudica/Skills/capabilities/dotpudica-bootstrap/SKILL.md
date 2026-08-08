---
name: dotpudica-bootstrap
description: Set up or verify the DotPudica framework inside a Godot .NET project: enabling the plugin, checking the injected csproj snippet, creating the AppContext bootstrap node (Initialize/Dispose), wiring GodotWindowManager. Use whenever the project has no bootstrap yet, the DotPudica plugin does not work, AppContext.Current throws, or before building any page or window. Also applies when a csproj is missing the DotPudica:Begin/End block.
---

# dotpudica-bootstrap

## Purpose

Integrate the DotPudica framework into a Godot .NET project and bootstrap the process-wide `AppContext` (IoC container + window manager + logging) so pages, windows, and `SceneContextHost` scenes can run. This is the entry point of the framework: nothing else works until `AppContext.Initialize` has completed.

## Input Contract

Preconditions:

- A `.csproj` exists (generated when the Godot C# project was created); Godot 4.7.x .NET editor + .NET 8 SDK.
- The project root has both `project.godot` and `.csproj`.

Valid input states:

- The `.csproj` contains the injected `<!-- DotPudica:Begin -->` ... `<!-- DotPudica:End -->` block referencing `addons/dot-pudica/Core`, `addons/dot-pudica/Godot`, `addons/dot-pudica/SourceGenerator` (as an Analyzer) plus `CommunityToolkit.Mvvm` 8.4.0 and `Microsoft.Extensions.DependencyInjection` 8.0.1.
- If the block is missing, first check that the plugin is enabled (Project Settings → Plugins → DotPudica); re-enabling the plugin injects the block automatically.

## Procedure

1. Verify the plugin is enabled (Project Settings → Plugins → DotPudica). If not, enable it (this also auto-injects the csproj block).
2. Verify the injected csproj block with:

```powershell
Select-String "DotPudica:Begin" <path-to-csproj>
```

If there is no match, disable and re-enable the plugin, then re-check.
3. Write the bootstrap node. Add this `GameBootstrap` (or equivalent) class to the project, attached to the main scene or registered as an Autoload:

```csharp
using DotPudica.Godot;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using AppContext = DotPudica.Godot.AppContext;

public partial class GameBootstrap : Node
{
    private AppContext? _app;

    public override void _EnterTree()
    {
        GodotWindowManager? wm = GetNodeOrNull<GodotWindowManager>("WindowManager");
        _app = new AppContext().Initialize(services =>
        {
            // services.AddSingleton<IInventoryService, InventoryService>();
        }, wm);
    }

    public override void _ExitTree()
    {
        _app?.Dispose();
        _app = null;
    }
}
```

4. Main scene node tree requirements:

- The bootstrap must live on the main scene's resident branch (or as an Autoload), not on a page that can be freed.
- The `WindowManager` child node may be placed in the editor, or created at runtime by the bootstrap itself (a private `EnsureWindowManager` helper that returns the existing `"WindowManager"` node or adds a new `GodotWindowManager` one). `Initialize` accepts a null `windowManagerNode`, but then `AppContext.WindowManager` and `IWindowManager` resolution throw.
- Checkpoints: `AppContext.Initialize` must finish before any `Show` / `ShowPooled` call and before any `SceneContextHost` enters the tree; `Initialize` may run only once per process (a repeated call throws).

## Output Contract

Deliverable = plugin enabled + the `DotPudica:Begin`/`End` block present in the `.csproj` + a `GameBootstrap` class that calls `Initialize` and `Dispose` exactly once each. Acceptance:

- `dotnet build` passes with no DOTPUDICA diagnostics (look up any that appear in `references/diagnostics.md`).
- Editor F5 runs without bootstrap exceptions.
- `AppContext.Current` is accessible and `AppContext.Current.Services` resolves.

## Failure Handling

| Symptom | Fix action |
|---|---|
| `AppContext.Current` throws `InvalidOperationException` ("AppContext is not initialized...") | No `Initialize` call has completed yet — write/verify the bootstrap and its placement in the scene tree. |
| `Initialize` throws on a second call ("can only be initialized once...") | The process already initialized — the call site is a duplicate; keep exactly one bootstrap. |
| `Select-String "DotPudica:Begin"` finds nothing in the `.csproj` | The plugin is not enabled or its injection ran before the project was fully reloaded — enable/re-enable the plugin (Project Settings → Plugins → DotPudica) and re-check. |
| `AppContext.WindowManager` access throws ("WindowManager is not configured...") | `Initialize` was called without the manager node — pass the `GodotWindowManager` node, or let the bootstrap create one. |

Compile-time DOTPUDICA diagnostics (e.g. `DOTPUDICA046` for missing `_Ready`/`_ExitTree` wiring) are covered in `references/diagnostics.md` — look them up there; this skill only handles bootstrap-time failures.

## References

- `references/api-tour.md` — section 1 (AppContext): `Initialize`/`Dispose`/`Current` signatures and constraints; section 3 (GodotWindowManager) for what the manager requires once wired.
- Related skills: `dotpudica-view` (pages/windows consume `AppContext.Current.Services`), `dotpudica-route` (chooses which skill a task needs).
