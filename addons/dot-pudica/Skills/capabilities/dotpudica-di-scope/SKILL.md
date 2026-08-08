---
name: dotpudica-di-scope
description: Register and consume services with DotPudica: AppContext configureServices callbacks (AddSingleton/AddTransient), [Inject] fields, [ViewModelFactory] methods, and SceneContextHost scene-level scopes with Operations cancellation tokens. Use when sharing data across pages, injecting services into views or ViewModels, isolating a scene's DI, or cancelling async work when leaving a scene.
---

# dotpudica-di-scope

## Purpose

Register services with the DotPudica container and consume them in pages and ViewModels, with the choice of a process-wide `AppContext` provider or a per-scene `SceneContextHost` scope. This is the I/O contract for service registration and scene-scoped DI: it defines the valid registration shapes, the three consumption routes (constructor injection, `[Inject]`, `[ViewModelFactory]`), the scene isolation pattern, and the compile-time and runtime failure modes.

## Input Contract

Preconditions:

- Bootstrap is done: `AppContext.Initialize` has completed (otherwise run `dotpudica-bootstrap` first). `SceneContextHost` builds its scope from `AppContext.Current.Services` in `_EnterTree` and throws if initialization has not finished.
- Service interfaces and implementations are plain .NET types (any assembly referenced by the project); they need no Godot base class and no DotPudica attribute.

Valid input states:

- Long-lived, cross-page services (profile, inventory, network client shared by many pages) register with `AddSingleton` — resolved from `AppContext.Current.Services`.
- Short-lived scene services and ViewModels that must not leak into other scenes register with `AddTransient` and are consumed through a `SceneContextHost` scope — resolved from `host.Scope.Services` / `host.Scope.ViewModels`.
- `[Inject]` target members: non-readonly field, or a property with a setter — otherwise `DOTPUDICA043`.
- `[ViewModelFactory]` method shape: parameterless, non-static, non-void instance method returning the declared ViewModel type or a derived type — otherwise `DOTPUDICA041`.
- A ViewModel is DI-resolvable without `[ViewModelFactory]` only when it has exactly one public constructor whose parameters are all interface types (and `AutoInitialize = true`) — otherwise `DOTPUDICA040`.

## Procedure

1. Decide the service lifetime: data shared across pages (or the whole process) → `AddSingleton`; state owned by one scene and its pages → `AddTransient` + a `SceneContextHost`.
2. Register services in the bootstrap's `Initialize` `configureServices` callback, for example:

```csharp
_app = new AppContext().Initialize(services =>
{
    services.AddSingleton<IProfileService, ProfileService>();
    services.AddTransient<MatchViewModel>();
}, wm);
```

3. Consume in a page, choosing one of three routes:

- Constructor injection: a ViewModel whose single public constructor takes interface parameters is constructed automatically (no factory needed).
- `[Inject]` field — resolved from `AppContext.Current.Services`:

```csharp
[DotPudicaView(typeof(ProfileViewModel))]
public partial class ProfilePage : Control
{
    [Inject]
    private IProfileService _profile = null!;
}
```

- `[ViewModelFactory]` method — for any other construction shape, or to pull from a scene scope (see step 5).

4. Scene isolation: manually attach a `SceneContextHost` node to the scene root — either by adding the script in the editor or at runtime:

```csharp
var host = new SceneContextHost { Name = "RoomScope" };
AddChild(host);
```

Put the scene's pages in its subtree. Checkpoint: `AppContext.Initialize` must finish before the host enters the tree (its `_EnterTree` reads `AppContext.Current.Services` and throws otherwise).
5. Create scene-scoped ViewModels through the host's scope factory:

```csharp
[DotPudicaView(typeof(MatchViewModel))]
public partial class MatchPage : Control
{
    [ViewModelFactory]
    private MatchViewModel CreateMatch()
    {
        var host = GetParent() as SceneContextHost
            ?? throw new InvalidOperationException("Place the page under a SceneContextHost");
        return host.Scope.ViewModels.Create<MatchViewModel>();
    }
}
```

6. Cancel scene-lifetime async work via `host.Operations` (`SceneOperationScope`): keep the token and use `CreateLinkedTokenSource()` for individual operations. The host disposes `Operations` first, then `Scope`, on `_ExitTree`.
7. Build with `dotnet build` and fix until zero DOTPUDICA diagnostics (look each ID up in `references/diagnostics.md`).

## Output Contract

Deliverable = the registration code block (in `configureServices`) + the consumption code block (`[Inject]` field or `[ViewModelFactory]` method) +, when isolating a scene, the `SceneContextHost` mount location with the pages in its subtree. Acceptance:

- `dotnet build` passes with zero DOTPUDICA diagnostics.
- Runtime checkpoints pass: every page consuming the same `AddSingleton` service resolves the same instance; a transient scene-scoped service is fresh per scene; after the scene exits, the scope is disposed — no cross-scene bleed (no stale singletons, no scope-disposed errors in a later scene); leaving a scene cancels in-flight operations via the scene token.

## Failure Handling

| Symptom | Fix action |
|---|---|
| `AppContext.Current` throws `InvalidOperationException` ("AppContext is not initialized...") | No `Initialize` call completed — write/verify the bootstrap (`dotpudica-bootstrap`); `SceneContextHost` also throws here if it enters the tree before initialization. |
| `DOTPUDICA040` (`ViewModelNotDiResolvable`) | The ViewModel is abstract or has no exactly-one-public-constructor-all-interface shape under `AutoInitialize = true` — satisfy the DI shape or add a `[ViewModelFactory]` method. |
| `DOTPUDICA041` (`ViewModelFactoryInvalid`) | The `[ViewModelFactory]` method is static, has parameters, returns void, or does not return the declared ViewModel type or a derived type — fix the method signature. |
| `DOTPUDICA043` (`InjectNotWritable`) | The `[Inject]` member is a readonly field or a property without a setter — make it writable. |
| `host.Scope` throws ("SceneScope has not been created yet...") or the `[ViewModelFactory]` host cast yields null | The page is not under a `SceneContextHost` subtree (or the host has not entered the tree) — mount a `SceneContextHost` at the scene root and put the page in its subtree. |
| `ServiceLocator.Configure` throws ("...already been configured. Call Reset() before configuring again.") | A second configuration attempt — `AppContext.Initialize` already configured it once per process; repeated config is a framework-internal/test pattern and needs `ServiceLocator.Reset()` first (for framework-internal and test use only). |

The table only maps triggers; the full trigger condition, verbatim message, and fix action for the diagnostics live in `references/diagnostics.md` — look the ID up there.

## References

- `references/api-tour.md` — section 1 (AppContext: `Initialize`/`Dispose`/`Current` constraints), section 2 (SceneContextHost: `Scope`/`Operations` behavior), section 11 (SceneOperationScope).
- `references/diagnostics.md` — `DOTPUDICA040`/`041`/`043` entries and the lifecycle matrix.
- Related skills: `dotpudica-bootstrap` (prerequisite: `AppContext.Initialize`), `dotpudica-view` (`[Inject]`/`[ViewModelFactory]` usage in pages), `dotpudica-route` (choosing which skill a task needs).
