---
name: dotpudica-wf-project-setup
description: End-to-end workflow: integrate DotPudica into a new Godot .NET project from scratch - enable the plugin, check the csproj injection, create the AppContext bootstrap, build a first minimal page, and verify. Use when starting a new project or when a project has no framework bootstrap yet.
---

# dotpudica-wf-project-setup

## Purpose

Take a new Godot .NET project with no framework presence and turn it into a running DotPudica application: plugin enabled, csproj block injected, process-wide `AppContext` bootstrapped, one minimal View + ViewModel page visible, verified end to end. This workflow owns the end-to-end task graph; the capability skills own the "how" of each node. After this workflow finishes, the project is ready for any further framework work routed through `dotpudica-route`.

## Trigger Patterns

Use this workflow when the user says:

- "New project" / "start a Godot project with DotPudica" / "integrate the framework".
- The project has no bootstrap yet — no `GameBootstrap`, `AppContext.Current` is not accessible, "framework not set up".
- The DotPudica plugin does not work — enabling it fails to inject the csproj block.
- A task states the project is "not set up" with DotPudica, even if it already has other framework code present.

Do not use it for adding pages, windows, or services to a project that already bootstrapped — route those to the add-page / add-window / shared-data workflows.

## Task graph

Execute the nodes in order; close each node's acceptance point before advancing.

- **N1 — Environment check** (deliverable: confirmed environment; invokes no skill): verify Godot 4.7.x .NET editor and .NET 8 SDK are installed and `dotnet build` runs in the project directory. Acceptance: a Godot C# project with `project.godot` + `.csproj` exists and `dotnet --version` reports 8.x.
- **N2 — Integrate the addon** (deliverable: plugin enabled; invokes `capabilities/dotpudica-bootstrap`): copy `addons/dot-pudica` into the project root and enable the plugin (Project Settings → Plugins → DotPudica). Acceptance: the `.csproj` now contains the `<!-- DotPudica:Begin -->` ... `<!-- DotPudica:End -->` block (checked with `Select-String "DotPudica:Begin"`).
- **N3 — Write the bootstrap** (deliverable: `GameBootstrap` class; invokes `capabilities/dotpudica-bootstrap`): a node on the main scene's resident branch calling `AppContext.Initialize` once, `Dispose` on exit, with an optional `GodotWindowManager` child node. Acceptance: `Initialize` runs exactly once per process and `AppContext.Current` is accessible.
- **N4 — Minimal page loop** (deliverable: one page; invokes `capabilities/dotpudica-view`): one ViewModel + one `[DotPudicaView]` View with the two lifecycle lines and every `[Export]` binding field assigned in the scene. Acceptance: `dotnet build` reports zero DOTPUDICA diagnostics.
- **N5 — Build verification + run check** (deliverable: zero-diagnostic build; invokes `capabilities/dotpudica-verify`): run the runtime checklist. Acceptance: the page shows its initial value (e.g. the Label bound to the ViewModel's initial string) on launch.

```powershell
Select-String "DotPudica:Begin" <project>.csproj
dotnet build
```

## Acceptance criteria

The workflow is complete when all of these hold:

- `dotnet build` completes with zero DOTPUDICA diagnostics.
- Running the project shows the first page with its initial value displayed.
- `AppContext.Current` is accessible and `AppContext.Current.Services` resolves.

## Failure branches

| Node | Symptom | Fix |
|---|---|---|
| N2 | `Select-String "DotPudica:Begin"` finds nothing in the `.csproj` | The plugin is not enabled or its injection ran before project reload — disable and re-enable the plugin, then re-check the block. |
| N4 | `DOTPUDICA040` / `DOTPUDICA041` | The ViewModel's construction shape is not all-interface — add a `[ViewModelFactory]` method matching the generator's expected shape. |
| N5 | Diagnostics remain after the fix round | Look each remaining ID up in `references/diagnostics.md`, fix the root cause, and rebuild — repeat until zero. |

Any node checkpoint failure returns to `capabilities/dotpudica-verify` to fix; never roll back and restart the graph from N1.

## References

- `capabilities/dotpudica-bootstrap` — N2 (plugin + csproj block) and N3 (bootstrap node, Initialize/Dispose contract).
- `capabilities/dotpudica-view` — N4 (View + ViewModel pair, lifecycle lines, `[ViewModelFactory]` shape).
- `capabilities/dotpudica-verify` — N5 (build command, diagnostic loop, runtime checklist).
- `dotpudica-route` — related: entry point that routes to this workflow; consult it for the next task on this project.
