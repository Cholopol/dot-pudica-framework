---
name: dotpudica-wf-fix-diagnostics
description: End-to-end workflow: fix DOTPUDICA* compile diagnostics - collect the build output, map each ID to its root cause (references/diagnostics.md), apply the fix, rebuild until clean, then re-run the original task's acceptance checks. Use whenever a build fails with DOTPUDICA codes or a diagnostic appeared after other framework work.
---

# dotpudica-wf-fix-diagnostics

## Purpose

Drive any `DOTPUDICA*` compile diagnostic to zero and prove the original task still works: collect the full build output, map every ID to its root cause in `references/diagnostics.md`, classify and fix each one through the owning capability skill, rebuild until clean, then return to the original task's acceptance point and re-run its checks. This workflow is the fallback for every workflow's failing branch — any graph that ends with diagnostics routes here. It owns the end-to-end task graph; `capabilities/dotpudica-verify` owns the "how" of each node.

## Trigger Patterns

Use this workflow when:

- A `dotnet build` fails with `DOTPUDICA*` error codes.
- A diagnostic appeared after other framework work (a new binding, service, window, pool, or scene-scope change introduced it).
- A verification step fails: the acceptance point of the originating skill (e.g. `dotpudica-verify`) reported a `DOTPUDICA` error and the task was returned here.

Do not use it for plain C# compile errors on code that never touches the framework (no `[DotPudicaView]`, no bindings) — those follow the project's normal compiler workflow; the only framework-related exception is a derived View, handled in Failure branches below.

## Task graph

Execute the nodes in order; close each node's acceptance point before advancing.

- **N1 — Collect** (deliverable: diagnostic list; invokes `capabilities/dotpudica-verify`): run `dotnet build` (full solution or the affected project) and capture every `DOTPUDICA*` error plus the plain C# errors around them. Acceptance: the list names every `DOTPUDICA` ID, its verbatim message, and the file/line it was reported on.
- **N2 — Locate** (deliverable: root-cause list; invokes no skill): for each ID, look up the row in `references/diagnostics.md` (section 1: trigger condition, verbatim message, fix action) and confirm the compiler message matches the row; read section 3 (diagnostic scope) before interpreting. Acceptance: each ID has exactly one identified offending member and source location.
- **N3 — Classify the root cause** (deliverable: fix plan): group the located errors by category — path errors (DOTPUDICA001/DOTPUDICA030), command errors (DOTPUDICA005), target-property errors (DOTPUDICA031), type errors (DOTPUDICA032/DOTPUDICA033/DOTPUDICA034/DOTPUDICA035), factory errors (DOTPUDICA040/DOTPUDICA041), lifecycle (DOTPUDICA046), subscribe (DOTPUDICA042), inject (DOTPUDICA043), collection (DOTPUDICA010/DOTPUDICA036/DOTPUDICA045) — and name the fix per `diagnostics.md` section 1. Acceptance: every ID is assigned to a category and a fix action, with no uncategorized leftover.
- **N4 — Fix** (deliverable: fixed source; invokes the owning capability skill per category — `dotpudica-view` for DOTPUDICA001/030 (binding paths), DOTPUDICA005 (command members), DOTPUDICA031 (explicit `[BindTo(Target=...)]` / `Signal` override), DOTPUDICA032/033/034/035 (converter and type issues), DOTPUDICA010/036 (ItemsSource bindings), and the DOTPUDICA046 lifecycle lines per the matrix in `diagnostics.md` section 2; `dotpudica-di-scope` for DOTPUDICA040/041/043; `dotpudica-view`/`dotpudica-messaging-threading` for DOTPUDICA042; `dotpudica-pooling` for DOTPUDICA045): apply the fixes one by one, respecting the referenced skill's contracts. Acceptance: each fix targets only the classified root cause and introduces no new problems (no unrelated edits, no new diagnostics visible on the next build round).
- **N5 — Rebuild** (deliverable: zero-diagnostic build; invokes `capabilities/dotpudica-verify`): run `dotnet build` again and confirm zero `DOTPUDICA` diagnostics; when the project builds with `-p:EmitCompilerGeneratedFiles=true`, also verify the generated artifacts (`obj/Generated/*.Bindings.g.cs`) contain the expected members. Acceptance: the build is clean, with no residual diagnostics or warnings caused by the fixes.
- **N6 — Regress** (deliverable: original acceptance passed): return to the original task's acceptance point (the workflow or capability skill that produced the failing code) and re-run its acceptance checks end to end. Acceptance: every original acceptance criterion passes again; if any fails, re-enter this graph from N1 with the new build output.

## Acceptance criteria

The workflow is complete when all of these hold:

- `dotnet build` completes with zero `DOTPUDICA` diagnostics.
- The original task's functional acceptance checks pass again (the behavior the diagnostics were blocking is restored).
- The fixes introduce no new diagnostics and no residue (unused code, abandoned bindings, or broken lifecycle wiring).

## Failure branches

| Node | Symptom | Fix |
|---|---|---|
| N5 | Build still reports `DOTPUDICA` errors after a fix round | Return to N2 and re-locate: re-check the ID's row in `diagnostics.md`, confirm the verbatim compiler message matches, and inspect the generated code before changing the fix — never guess a fix without a re-located root cause. |
| N5 | Only bare C# errors remain, no `DOTPUDICA` number | Derived-View scenario: the view inherits `[DotPudicaView]` from a base class, so its binding errors surface as plain C# errors — manually inspect the base class's bindings and generated code instead of looking for a diagnostic ID. |
| N2/N5 | A diagnostic is expected on a derived View but none is reported | The generator only reports diagnostics for the view that itself declares `[DotPudicaView]` (it owns the DotPudica runtime — `OwnsDotPudicaRuntime`); a derived view's errors are plain C# errors — verify the base-class binding and the derived class's generated code manually, per `diagnostics.md` section 3. |

Any node checkpoint failure returns to the node's invoked capability skill to fix; never roll back and restart the graph from N1.

## References

- `references/diagnostics.md` — N2 (all 16 diagnostics: trigger condition, verbatim message, fix action), N3 (category classification), N4 (the `DOTPUDICA046` lifecycle matrix), and diagnostic scope (`OwnsDotPudicaRuntime`).
- `capabilities/dotpudica-verify` — N1 (build command, diagnosis loop), N5 (zero-diagnostic acceptance, generated-artifact assertions).
- `dotpudica-route` — related: its decision table routes any "build fails with DOTPUDICAxxx" request to this workflow and describes its capability sequence (verify → back to origin skill).
