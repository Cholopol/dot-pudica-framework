---
name: dotpudica-wf-pooling
description: End-to-end workflow: optimize frequently opened and closed panels and popups with DotPudica pooling - decide the mode (pooled view / pooled window / ItemsSource PoolSize), apply [DotPudicaView(Pooled = true)] or ConfigurePool/ShowPooled, and verify the improvement. Use when the user reports stutter, lag, or GC pressure from panels, popups, or item lists.
---

# dotpudica-wf-pooling

## Purpose

Take a stuttering or GC-heavy panel, popup, or item list and remove its create-destroy churn with DotPudica pooling, end to end: locate the hotspot, decide the pooling mode from the decision table, refactor the view and its holder, and prove the improvement through build and runtime checks. This workflow owns the end-to-end task graph; `capabilities/dotpudica-pooling` owns the "how" of each node. It covers the pooling side of the framework — pooled views, pooled windows through the `GodotWindowManager`, and `[ItemsSource]` row recycling.

## Trigger Patterns

Use this workflow when the user says:

- "Opening/closing this panel stutters" / "this popup lags on open".
- "GC pressure" / "allocation churn" / "the profiler spikes when I open and close the dialog".
- "Scrolling this list is janky" — frequent create/destroy of item rows.
- Code that hand-rolls pooling — `new` + `QueueFree` per open instead of a pool — or a frequently opened view that still ends in `DisposeView`.

Do not use it for a one-shot popup opened a handful of times (plain `DisposeView` + `QueueFree` is fine) or for a view created once and kept alive for the app lifetime (there is no churn to pool). Route those to `dotpudica-wf-add-window` / `dotpudica-wf-add-page` instead.

## Task graph

Execute the nodes in order; close each node's acceptance point before advancing.

- **N1 — Clarify the hotspot** (deliverable: hotspot list; invokes no skill): identify which panel, popup, or item list is created and destroyed frequently, how often it opens/closes, and who owns its ViewModel — the view self-creates its VM (default `AutoInitialize = true`) or a caller supplies a shared instance (`AutoInitialize = false`). Acceptance: the list names the hot controls, their open frequency, and the VM ownership of each — no open questions.
- **N2 — Decide the pooling mode** (deliverable: mode conclusion; invokes `capabilities/dotpudica-pooling`): from the decision table — view pool with self-created VM (`AutoInitialize = true` + `Pooled = true`); view pool with shared VM (`AutoInitialize = false` + `ActivateViewModel` per allocation); window pool (`wm.ConfigurePool<T>` / `wm.ShowPooled<T>`); item list rows (`[ItemsSource] PoolSize = n`, never on `VirtualizedItemsControl`). Acceptance: the chosen mode matches the N1 hotspot and its VM ownership, and one-shot popups are explicitly excluded from pooling.
- **N3 — Refactor the view** (deliverable: pooled view; invokes `capabilities/dotpudica-pooling`): `[DotPudicaView(typeof(TVM), Pooled = true)]` with `_ExitTree() => RecycleView()` (keep `_Ready() => InitializeView()`). Acceptance: the view builds with zero DOTPUDICA diagnostics — no `DOTPUDICA046` from a missing `_ExitTree` or one that still calls `DisposeView`.
- **N4 — Refactor the holder** (deliverable: borrow/return code; invokes `capabilities/dotpudica-pooling`): `NodePool.Create<T>(maxSize)` with `Allocate()`/`Free(view)`, or `wm.ConfigurePool<T>(maxSize)` once before `wm.ShowPooled<T>()` for windows. Acceptance: every pooled object returns through `pool.Free(view)` / `wm.Dismiss(window)` — no direct `QueueFree` of a pooled object.
- **N5 — Build and runtime check** (deliverable: build result + instance counts; invokes `capabilities/dotpudica-verify`): `dotnet build` with zero diagnostics, then launch and open/close the hot control N times in a row. Acceptance: the node instance count does not grow across the N cycles (the same instances are reused), a reused pooled window gets a fresh ViewModel on every show, and a shared-VM pooled view keeps the VM alive across recycles.

```powershell
dotnet build
Select-String -Path <view>.cs -Pattern "RecycleView"
```

## Acceptance criteria

The workflow is complete when all of these hold:

- `dotnet build` completes with zero DOTPUDICA diagnostics.
- After opening and closing the hot control N times in a row, the instance count is stable — no growth and no leak.
- Behavior matches the pre-pooling state: data binds, subscriptions fire, commands work — pooling only changes allocation behavior.
- One-shot popups and `VirtualizedItemsControl` lists are left untouched.

## Failure branches

| Node | Symptom | Fix |
|---|---|---|
| N4 | `ShowPooled` throws "No window pool configured for ...; call ConfigurePool first." | `ConfigurePool<T>(maxSize)` was never called — add it once before the first `ShowPooled<T>`, then re-run N5. |
| N5 | Instance count still grows across open/close cycles | A code path bypasses the pool — some create/destroy does not go through `pool.Allocate`/`pool.Free` or `ShowPooled`/`Dismiss`; find it and route it through the pool, then re-verify. |
| N5 | State residue after pooling — stale data or bindings on the reopened view | `RecycleView` is not declared in `_ExitTree` (or `DisposeView` is still used) — the node re-enters the tree without teardown/re-arm; declare `_ExitTree() => RecycleView()` on the pooled view and rebuild. |

Any node checkpoint failure returns to the invoked capability skill to fix; never roll back and restart the graph from N1.

## Common variants

- **List row pooling**: apply `[ItemsSource] PoolSize = n` to the non-virtualized list so rows recycle instead of recreate; never set `PoolSize` on a `VirtualizedItemsControl` — it manages its own rows and rejects `PoolSize` with `DOTPUDICA045` (`capabilities/dotpudica-pooling`).
- **Shared-VM pooled view**: when several pooled views bind to one shared ViewModel instance, set `AutoInitialize = false` and call `ActivateViewModel(vm)` on every allocation; the VM survives each recycle because ownership is `External`.
- **Pooled windows with payloads**: pass data through `ShowPooled<T>(IBundle?)` so each recycled show re-reads the `OnCreate` hook with the fresh Bundle.

## References

- `capabilities/dotpudica-pooling` — N2 (decision table), N3 (pooled view shape, `RecycleView`), N4 (`NodePool.Create`/`Allocate`/`Free`, `ConfigurePool`/`ShowPooled`), `DOTPUDICA045`/`046` failure rows.
- `capabilities/dotpudica-verify` — N5 (build command, diagnostic loop, runtime checklist, instance-count verification).
- `capabilities/dotpudica-view` — related: view declaration, `AutoInitialize`, `[ItemsSource]` semantics the pooling modes build on.
- `dotpudica-route` — related: the routing decision table routes "stutter, lag, or GC pressure from panels, popups, or item lists" to this workflow.
