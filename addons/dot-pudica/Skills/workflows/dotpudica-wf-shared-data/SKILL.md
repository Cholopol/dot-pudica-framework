---
name: dotpudica-wf-shared-data
description: End-to-end workflow: deliver cross-page shared state with DotPudica singleton services - design the service interface, register it in AppContext, inject it into pages, and verify. Use when the user wants data shared between pages (player profile, inventory, match settings...) or complains that each page has its own copy of state.
---

# dotpudica-wf-shared-data

## Purpose

Take a cross-page shared-state requirement and deliver it end to end: a service interface and plain .NET implementation, one `AddSingleton` registration in the bootstrap's `configureServices`, injection into the pages that need it, optional binding of page UI to the service data, and verification through build and runtime. This workflow owns the end-to-end task graph; `capabilities/dotpudica-di-scope` owns the "how" of registration and injection.

## Trigger Patterns

Use this workflow when the user says:

- "Share X between pages" / "global state" / "the profile must be visible on every page" / "inventory across screens".
- Multiple pages each hold their own copy of the same data and updates do not propagate.
- A page needs the same long-lived data on every visit (player profile, inventory, match settings, app settings).

Do not use it when the data lives in only one scene and must be torn down on leaving (route to `dotpudica-wf-scene-scope`), or when the project has no bootstrap yet (route to `dotpudica-wf-project-setup` first).

## Task graph

Execute the nodes in order; close each node's acceptance point before advancing.

- **N1 — Clarify what is shared** (deliverable: requirement summary; invokes no skill): which data is shared, who writes and who reads it, and whether the lifetime is process-level (a page list on the profile is short-lived state and does not belong here). Acceptance: the summary names the data, the writer and reader pages, and confirms process-level lifetime — no open questions.
- **N2 — Design the service** (deliverable: interface and implementation; invokes no skill): an interface plus a plain .NET class implementing it — no Godot base class, no DotPudica attribute, no performance tuning. Include `INotifyPropertyChanged` on the implementation only if pages bind to the data. Acceptance: the interface and implementation compile standalone and the design satisfies N1's data and accessors.
- **N3 — Register the singleton** (deliverable: registration line; invokes `capabilities/dotpudica-di-scope`): `services.AddSingleton<IProfileService, ProfileService>();` inside the `AppContext.Initialize` `configureServices` callback of the bootstrap. Acceptance: the registration point exists in `configureServices` and matches the interface from N2.
- **N4 — Inject and consume** (deliverable: consumption code; invokes `capabilities/dotpudica-di-scope`): the page (or its ViewModel) takes the service through constructor injection, or a `[Inject]` field on the View. Acceptance: the field is writable (no `DOTPUDICA043`) and resolves at runtime.
- **N5 — Bind pages to the service data (on demand)** (invokes `capabilities/dotpudica-view`): the ViewModel holds the service reference and forwards its data as observable properties, and the View binds to those properties. Acceptance: the notified object (implementation or ViewModel) raises `PropertyChanged` and every binding path resolves on the ViewModel.
- **N6 — Build and runtime check** (deliverable: check conclusion; invokes `capabilities/dotpudica-verify`): `dotnet build` with zero DOTPUDICA diagnostics, then launch and compare the two pages. Acceptance: both pages read the same instance and a change made on one page is visible on the other.

```powershell
dotnet build
```

## Acceptance criteria

The workflow is complete when all of these hold:

- `dotnet build` completes with zero DOTPUDICA diagnostics.
- Two pages inject the same service instance (singleton identity confirmed).
- A state change made on one page is visible on the other (via property notification when bound).
- The service is registered exactly once per process — a second `AppContext.Initialize` throws; keep a single call site.

## Failure branches

| Node | Symptom | Fix |
|---|---|---|
| N3 | Duplicate registration throws ("already been configured") | `AppContext.Initialize` must be called exactly once — check the bootstrap's `Initialize` call site and remove the second call. |
| N4 | The service cannot be resolved | Check the registration name and interface match — the `AddSingleton<TInterface, TImpl>` generic arguments must match the injected type exactly. |
| N6 | Data does not sync between pages | Check the service is registered as `AddSingleton` (not `AddTransient`), and that the implementation raises `PropertyChanged` / the ViewModel forwards it when pages bind to the data. |

Any node checkpoint failure returns to `capabilities/dotpudica-verify` to fix; never roll back and restart the graph from N1.

## References

- `capabilities/dotpudica-di-scope` — N3 (registration in `configureServices`) and N4 (constructor injection / `[Inject]` contract, failure modes `DOTPUDICA040`/`041`/`043`).
- `capabilities/dotpudica-view` — N5 (ViewModel observable properties, binding paths, lifecycle lines).
- `capabilities/dotpudica-verify` — N6 (build command, diagnostic loop, runtime checklist).
- `dotpudica-route` — related: entry point that routes to this workflow; consult it for the next task on this project.
