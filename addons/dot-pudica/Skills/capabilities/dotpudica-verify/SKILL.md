---
name: dotpudica-verify
description: Verify and fix DotPudica code: run dotnet build, interpret DOTPUDICA* diagnostics (001/005/010/030-036/040-043/045/046), inspect generated .Bindings.g.cs output, and apply the framework's common-pitfall fixes. Use at the end of every task that wrote framework code and whenever a build error message contains DOTPUDICA, a binding silently does nothing, or a View is missing its _Ready/_ExitTree lines.
---

# dotpudica-verify

## Purpose

Prove that DotPudica code builds clean and behaves correctly at runtime, and drive any DOTPUDICA* compile error to zero. This is the I/O contract for verification and diagnosis: it defines the exact build command, the diagnosis loop (diagnostic ID → reference → root cause → fix → rebuild), the generated-artifact assertions, the runtime checklist, and the framework's common-pitfall fixes. Every workflow skill ends here: run it at the end of every task that wrote framework code, whenever a build error message contains `DOTPUDICA`, whenever a binding silently does nothing, and whenever a View is missing its `_Ready` / `_ExitTree` lines.

## Input Contract

Preconditions:

- The framework csproj block is injected and the project builds structurally (see `dotpudica-bootstrap`).
- All 16 diagnostics are documented in `references/diagnostics.md`; read that file before interpreting any diagnostic ID.

Valid input states:

- Written framework code: a `[DotPudicaView]` View + ViewModel pair (or window / pooled view) with declared bindings, subscriptions, and lifecycle lines.
- Build output: compile errors and warnings from `dotnet build`, including any `DOTPUDICA*` diagnostics.

Symptom set this skill handles:

- Build reports a `DOTPUDICA*` error.
- A binding silently does nothing (UI never reflects the ViewModel).
- A page is blank or its ViewModel never binds.
- A cross-thread exception at runtime.
- A displayed value never updates after the ViewModel changes.

## Procedure

1. **Build** — run `dotnet build` (full solution or the affected project) and collect every `DOTPUDICA*` error plus all plain C# errors. Godot's command line does not rebuild assemblies automatically, so a `dotnet build` must always precede any headless integration run.
2. **Diagnose and fix** — for each diagnostic ID: look it up in `references/diagnostics.md` (section 1 table: trigger condition, verbatim message, fix action) → identify the root cause in the source → apply the fix. Fix the reported diagnostics one by one, rebuilding after each round, until the build reports zero `DOTPUDICA` diagnostics.
3. **Cross-check the generated artifacts (optional)** — only when the project builds with `-p:EmitCompilerGeneratedFiles=true` (or the csproj sets it) does the generator emit files under `obj/Generated`, one per `[DotPudicaView]` view:

```powershell
Get-ChildItem -Recurse obj/Generated -Filter "*.Bindings.g.cs" | Select-Object -ExpandProperty FullName
```

For each view, verify `{Namespace}.{Class}.Bindings.g.cs` exists and contains the expected members: `InitializeView` / `DisposeView` (or `RecycleView` when `Pooled = true`; `ActivateViewModel` when pooled + `AutoInitialize = false`), plus the generated binding statements for every declared `[BindTo]` / `[BindCommand]` / `[ItemsSource]`. A missing file or missing members means the generator did not run — fix the source shape before proceeding. A plain `dotnet build` (without `EmitCompilerGeneratedFiles`) emits nothing to `obj/Generated`, so its absence is not a failure — rely on zero diagnostics plus the runtime checklist below.
4. **Run the runtime checklist** (generic, apply to any page) — after a successful build and launch:

- Initial values display correctly on open.
- Commands are triggerable: a button `pressed` event fires the bound command.
- Background data updates reach the UI.
- No leaks after closing the page: subscriptions, windows, and pool entries leave no residue when the View leaves the tree (bindings are released by `DisposeView` / `RecycleView`).

5. **Consult the common-pitfall quick table** when a step above fails without a diagnostic ID:

| Symptom | Root cause | Fix |
|---|---|---|
| `DOTPUDICA046` | `_Ready` / `_ExitTree` overrides missing, or present without the call | Add `public override void _Ready() => InitializeView();` and `public override void _ExitTree() => DisposeView();` (use `RecycleView()` when `Pooled = true`) |
| Binding silently does nothing | Control field not assigned in the scene inspector; binding path typo or wrong generated member name (`DOTPUDICA001`); event-name inference failed | Assign every `[Export]` binding field a node; fix the path to the generated name; override the `Signal` inference explicitly |
| Page blank / ViewModel never binds | The two lifecycle lines missing; `AutoInitialize = false` without a manual `SetViewModel` | Add the `_Ready` / `_ExitTree` lines; call `SetViewModel(vm, ...)` + `DotPudicaInitialize()` manually |
| Cross-thread exception | Background thread mutates controls or bound collections directly | Marshal with `IUiDispatcher.Post` captured on the main thread |
| Value display does not update | `BindingMode.OneTime` binding; ViewModel property has no `[ObservableProperty]` | Use `OneWay`/`TwoWay` as needed; add `[ObservableProperty]` to the state field |

## Output Contract

Deliverable = the fixed source code + a zero-diagnostic build result + a runtime-checklist conclusion stating which checks passed. Acceptance:

- `dotnet build` completes with zero `DOTPUDICA` diagnostics.
- Every runtime checklist item from Procedure step 4 passes and the conclusion is recorded in the task report.

## Failure Handling

- **Unknown errors remain after all fixes** — re-enter the loop: look the diagnostic ID up in `references/diagnostics.md` again, confirm the verbatim compiler message matches the row, and re-check the root cause in the generated code before changing the fix.
- **Diagnostic not reported on a derived View** — diagnostics are only reported for the view that itself declares `[DotPudicaView]`; a derived view's binding errors surface as plain C# compile errors without a `DOTPUDICA` number. Manually inspect the base class's bindings and generated code instead.
- **Build passes but runtime behavior is wrong** — treat it as a new symptom and re-run the runtime checklist (Procedure step 4) with the common-pitfall table.

## References

- `references/diagnostics.md` — all 16 diagnostics: trigger condition, verbatim message, fix action, the `DOTPUDICA046` lifecycle matrix, and diagnostic scope.
- `references/lifecycle.md` — `InitializeView` / `DisposeView` / `RecycleView` / `ActivateViewModel` step order and the `AutoInitialize` x `Pooled` matrix.
- Related skills: `dotpudica-view` (writing the View + ViewModel being verified), `dotpudica-route` (choosing which skill a task needs).
