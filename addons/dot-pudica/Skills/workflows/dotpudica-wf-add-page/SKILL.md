---
name: dotpudica-wf-add-page
description: End-to-end workflow: deliver a complete new UI page (settings, inventory, menu, any panel) with ViewModel, View, bindings, commands, optional item lists, and verification. The most common workflow - use whenever the user describes a page or panel they want to build.
---

# dotpudica-wf-add-page

## Purpose

Take a described page or panel and deliver it end to end: a ViewModel holding state, commands, and events, a `[DotPudicaView]` View with declarative bindings and the two lifecycle lines, optional item lists, and verification through build and runtime. This workflow owns the end-to-end task graph; the capability skills own the "how" of each node. It is the most common workflow — most framework work is "add this page".

## Trigger Patterns

Use this workflow when the user says:

- "Build a settings page" / "inventory screen" / "menu" / "XX panel" / "a screen that shows Y".
- A page exists but has no bindings — it is built with manual `Label.Text = ...` plumbing instead of ViewModel bindings.
- An existing page needs a list or command added (item list, new button behavior).

Do not use it for popups or dialogs hosted as windows (route to `dotpudica-wf-add-window`) or for projects with no bootstrap (route to `dotpudica-wf-project-setup` first).

## Task graph

Execute the nodes in order; close each node's acceptance point before advancing.

- **N1 — Clarify the requirement** (deliverable: requirement summary; invokes no skill): page purpose, data source (which service supplies it), interactions (what the user can do), and whether the page lives inside a window/popup. Acceptance: the summary names the page type, its data, and its interactions — no open questions.
- **N2 — Verify the bootstrap** (deliverable: readiness confirmed; invokes `capabilities/dotpudica-bootstrap` for the check): the csproj contains the `<!-- DotPudica:Begin -->` ... `<!-- DotPudica:End -->` block and a bootstrap class calls `AppContext.Initialize` once. If missing, run `dotpudica-wf-project-setup` first. Acceptance: `Select-String "DotPudica:Begin"` matches and `AppContext.Current` is reachable.
- **N3 — Design the ViewModel** (deliverable: VM class; invokes `capabilities/dotpudica-view`): state as `[ObservableProperty]` fields, commands as `[RelayCommand]` methods, events the page must react to. Acceptance: the class is `partial`; construction is DI-resolvable (exactly one public all-interface constructor, no `DOTPUDICA040` needed) or a `[ViewModelFactory]` method is declared.
- **N4 — Write the View** (deliverable: View class; invokes `capabilities/dotpudica-view`): `[DotPudicaView(typeof(TVM))]`, `[Export]` control fields carrying the binding attributes, and the two lifecycle lines `_Ready() => InitializeView()` / `_ExitTree() => DisposeView()`. Acceptance: every binding path resolves on the ViewModel and every `[Export]` binding field is assigned a node in the scene.
- **N5 — Add lists/commands (on demand)** (deliverable: list/command bindings; invokes `capabilities/dotpudica-view`): `[ItemsSource(path, itemScene)]` for collections, `[BindCommand]` for per-item or extra commands. Acceptance: the collection implements `INotifyCollectionChanged` and the `ItemCommand` parameter type matches the element type.
- **N6 — Build verification** (deliverable: build result; invokes `capabilities/dotpudica-verify`): `dotnet build` with zero DOTPUDICA diagnostics; when the project builds with `-p:EmitCompilerGeneratedFiles=true`, also confirm the generated artifacts. Acceptance: zero diagnostics; with `EmitCompilerGeneratedFiles` enabled, `obj/Generated` contains the `Bindings.g.cs` with `InitializeView` / `DisposeView`.
- **N7 — Runtime check** (deliverable: check conclusion; invokes `capabilities/dotpudica-verify`): launch the project, confirm initial values display, trigger an interaction, then leave the page. Acceptance: initial state shows, commands fire, and leaving the page produces no leaks or repeated-subscription errors.

```powershell
Select-String "DotPudica:Begin" <project>.csproj
dotnet build
```

## Acceptance criteria

The workflow is complete when all of these hold:

- `dotnet build` completes with zero DOTPUDICA diagnostics.
- The page displays the ViewModel's initial state on open.
- Commands trigger visible effects (button press updates state / UI).
- Leaving the page leaks nothing — returning does not duplicate subscriptions or error.

## Failure branches

| Node | Symptom | Fix |
|---|---|---|
| N3 | `DOTPUDICA040` / `DOTPUDICA041` | The ViewModel's construction shape is not all-interface — add a `[ViewModelFactory]` method matching the generator's expected shape. |
| N4 | `DOTPUDICA046` | Add the two lifecycle lines: `_Ready() => InitializeView()` and `_ExitTree() => DisposeView()` (use `RecycleView()` when `Pooled = true`). |
| N4 | `DOTPUDICA001` | A binding path does not resolve — fix the path or member name (Mvvm members must use generated names: `Title`, `SaveCommand`). |
| N6 | Diagnostics remain after the fix round | Look each remaining ID up in `references/diagnostics.md`, fix the root cause, and rebuild — repeat until zero. |

Any node checkpoint failure returns to `capabilities/dotpudica-verify` to fix; never roll back and restart the graph from N1.

## Common variants

- **Page with a list**: N5 uses `[ItemsSource]` with an item scene plus an `ItemCommand` for item taps; each item scene is itself a small View whose binding path is the element type.
- **Popup embedded in the page**: after N4, additionally invoke `capabilities/dotpudica-windows` for the popup class and its scene path; the page shows it via the window manager.

## References

- `capabilities/dotpudica-view` — N3 (ViewModel shape, `[ObservableProperty]` / `[RelayCommand]` generated names) and N4/N5 (View class, binding attributes, lifecycle lines, `[ItemsSource]` contract).
- `capabilities/dotpudica-bootstrap` — N2 (csproj block check, bootstrap readiness).
- `capabilities/dotpudica-verify` — N6/N7 (build command, diagnostic loop, runtime checklist).
- `capabilities/dotpudica-windows` — variant: popups embedded in the page.
- `dotpudica-route` — related: entry point that routes to this workflow; consult it for the next task on this project.
