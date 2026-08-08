---
name: dotpudica-wf-scene-scope
description: End-to-end workflow: deliver scene-isolated DI and leave-scene cancellation with SceneContextHost - mount the host, register transient services, create ViewModels from host.Scope, pass Operations tokens to async work, and verify. Use for rooms, matchmaking, or any scene with its own DI boundary and async work that must stop when the player leaves.
---

# dotpudica-wf-scene-scope

## Purpose

Take a scene-isolation requirement and deliver it end to end: a `SceneContextHost` mounted at the scene root, transient scene services registered in the bootstrap, ViewModels created from `host.Scope.ViewModels`, async work wired to the host's `Operations` token so leaving the scene cancels it, and verification through build and runtime. This workflow owns the end-to-end task graph; `capabilities/dotpudica-di-scope` owns the "how" of the host, registration, and scope creation; `capabilities/dotpudica-messaging-threading` owns the "how" of the cancellation wiring.

## Trigger Patterns

Use this workflow when the user says:

- "Build a room / match / level scene" — a scene with its own DI boundary, where services and ViewModels belong to that scene only.
- "Cancel the request when the player leaves" / "stop the operation on exit" — async work (network calls, timers) that must stop when the scene is left.
- "Scene-level short-lived ViewModels/services" — state that must be torn down with the scene and must not leak into other scenes.

Do not use it when the data must survive across pages and scenes (route to `dotpudica-wf-shared-data`), or when the project has no bootstrap yet (route to `dotpudica-wf-project-setup` first).

## Task graph

Execute the nodes in order; close each node's acceptance point before advancing.

- **N1 — Clarify the scene boundary** (deliverable: requirement summary; invokes no skill): which scene owns the boundary, which ViewModels and services are scene-level, and which async operations must be cancelled on leaving. Acceptance: the summary names the scene, its scene-scoped services and ViewModels, and the cancellable async work — no open questions.
- **N2 — Mount the host** (deliverable: host mounted; invokes `capabilities/dotpudica-di-scope`): attach a `SceneContextHost` to the scene root — in the editor or at runtime with `AddChild` — and put the scene's pages in its subtree. Acceptance: `AppContext.Initialize` completes before the host enters the tree (its `_EnterTree` builds the scope from `AppContext.Current.Services` and throws otherwise).
- **N3 — Register the scene services** (deliverable: registration lines; invokes `capabilities/dotpudica-di-scope`): `services.AddTransient<T>()` for the scene-level ViewModels and services in the bootstrap's `configureServices`. Acceptance: every scene-level type from N1 is registered as `AddTransient` — never `AddSingleton`.
- **N4 — Create ViewModels through the scope** (deliverable: factory method; invokes `capabilities/dotpudica-di-scope`): the page declares a `[ViewModelFactory]` that resolves the host from its parent and returns `host.Scope.ViewModels.Create<T>()`. Acceptance: the page sits in the host's subtree (the `GetParent() as SceneContextHost` cast resolves) and the factory matches the generator's expected shape.
- **N5 — Wire cancellation** (deliverable: cancellation wiring; invokes `capabilities/dotpudica-messaging-threading`): pass `host.Operations.Token` into the long async calls, and give each individual operation its own `CreateLinkedTokenSource()` so cancelling one operation does not cancel the whole scene. Acceptance: every long-running call observes a scene-lifetime token and the `OperationCanceledException` path does not update the UI.
- **N6 — Build and runtime check** (deliverable: check conclusion; invokes `capabilities/dotpudica-verify`): `dotnet build` with zero DOTPUDICA diagnostics, then launch: enter the scene, leave it with async work in flight, and re-enter. Acceptance: entering creates a new scope, leaving cancels the in-flight work, and no state bleeds across visits.

```powershell
dotnet build
```

## Acceptance criteria

The workflow is complete when all of these hold:

- `dotnet build` completes with zero DOTPUDICA diagnostics.
- Entering the scene creates a fresh scope each visit; entering and leaving several times leaks nothing — no accumulating duplicate ViewModels.
- Leaving the scene cancels in-flight async work; after leaving, callbacks no longer update the UI.
- No cross-scene bleed — scene A's services and ViewModels never appear in scene B.

## Failure branches

| Node | Symptom | Fix |
|---|---|---|
| N2 | Getting the host throws, or the factory's host cast yields null | The page is not under a `SceneContextHost` subtree (or the host entered the tree before initialization) — mount the host at the scene root and put the page in its subtree. |
| N4 | `DOTPUDICA041` | The `[ViewModelFactory]` method is static, has parameters, returns void, or does not return the declared ViewModel type or a derived type — fix the method signature. |
| N6 | Cross-scene bleed (scene A state shows in scene B) | A scene-level service was registered as `AddSingleton` by mistake — change it to `AddTransient` so each scene resolves fresh instances. |

Any node checkpoint failure returns to `capabilities/dotpudica-verify` to fix; never roll back and restart the graph from N1.

## References

- `capabilities/dotpudica-di-scope` — N2 (SceneContextHost mount and `_EnterTree` scope build) and N3/N4 (`AddTransient` registration, `host.Scope.ViewModels.Create<T>()`, `[ViewModelFactory]` contract and failure modes).
- `capabilities/dotpudica-messaging-threading` — N5 (`SceneOperationScope`: `Token`, `CreateLinkedTokenSource`, dispose-on-exit semantics).
- `capabilities/dotpudica-view` — related: ViewModel/View shape used by N4's factory and the page lifecycle lines.
- `capabilities/dotpudica-verify` — N6 (build command, diagnostic loop, runtime checklist).
- `dotpudica-route` — related: entry point that routes to this workflow; consult it for the next task on this project.
