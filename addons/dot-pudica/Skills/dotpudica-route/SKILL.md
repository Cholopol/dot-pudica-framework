---
name: dotpudica-route
description: Entry point for any task involving the DotPudica MVVM framework (Godot .NET): creating UI pages, bindings, commands, item lists, windows or popups, shared services, scene scopes, object pooling, or fixing DOTPUDICA compile diagnostics. Consult this routing skill FIRST whenever the task touches addons/dot-pudica, [DotPudicaView], [BindTo], [BindCommand], [ItemsSource], AppContext, SceneContextHost, GodotWindowManager, NodePool, or any DOTPUDICA* error - even if you already know the answer.
---

# dotpudica-route

## Purpose

Single entry point for the whole DotPudica skill set. Its product is a **task graph**: an ordered sequence of skill invocations, the parameters passed between them, and a checkpoint (the invoked skill's Output Contract) at every node. You execute the graph node by node — invoke the skill, verify its Output Contract, then advance.

```text
Task: "confirm dialog must show the current user name"
 1. dotpudica-bootstrap (check) — confirm AppContext.Initialize completed
    checkpoint: AppContext.Current.Services resolves the user service
 2. dotpudica-windows — window type + scene path + bundle payload
    checkpoint: window opens and shows the bundle data
 3. dotpudica-verify — build + runtime checkpoints
    checkpoint: zero DOTPUDICA diagnostics
```

## Usage rules (hard discipline)

1. **Route before you execute**: any task touching the framework is routed through this skill first — never jump straight into writing code.
2. **Single-capability small tasks** (one binding, one service, one event, one build error) link directly to the matching capability skill; skip the workflow layer.
3. **Multi-capability tasks** route to the matching workflow skill, which owns the end-to-end task graph.
4. **Close every invoked skill** by checking its Output Contract before starting the next node.

## Decision table

| User says (examples) | Route to | Capability sequence |
|---|---|---|
| New project / integrate framework / no bootstrap | dotpudica-wf-project-setup | bootstrap → view → verify |
| Build a settings page / new page / panel | dotpudica-wf-add-page | view → verify |
| Popup / confirm dialog / full-screen page | dotpudica-wf-add-window | bootstrap(check) → windows → verify |
| Share data between pages / global state | dotpudica-wf-shared-data | bootstrap(check) → di-scope → verify |
| Room / match scene isolation, cancel on leave | dotpudica-wf-scene-scope | di-scope → messaging-threading → verify |
| Panels lag / frequent open-close / performance | dotpudica-wf-pooling | pooling → view → verify |
| Build fails with DOTPUDICAxxx | dotpudica-wf-fix-diagnostics | verify → (back to origin skill) |
| A single binding does not work | (direct) dotpudica-view | view (+ verify as needed) |
| A service cannot be resolved | (direct) dotpudica-di-scope | di-scope (+ verify as needed) |
| Subscribe to events / async UI updates | (direct) dotpudica-messaging-threading | messaging-threading (+ verify as needed) |
| AppContext throws / plugin not working | (direct) dotpudica-bootstrap | bootstrap → verify |

## Fallback rule

An uncovered request is not a failure exit. Identify the capability domains involved, then generate a task graph following the collaboration order below. The capability sequence always ends with a `verify` tail.

## Collaboration order (hard rules)

- **bootstrap always first**: `AppContext.Initialize` must complete before any `Show`/`ShowPooled` call and before any `SceneContextHost` enters the tree.
- `ConfigurePool` must run before `ShowPooled`.
- A `SceneContextHost` mounts before its subtree pages are built.
- After any skill changes code, the tail is always `dotpudica-verify`.

## Parameter passing convention

- Skills exchange only requirement context: type names, scene paths, service interface names. Never pass unverified code artifacts between skills.
- A node checkpoint failure returns to `dotpudica-verify` to fix — never roll back and redo from scratch.

## When not to use

- Pure Godot native functionality (no bindings, no ViewModel) — the skill set does not apply.
- A simple demo/prototype — write it by hand; the philosophy is to stack capabilities on demand, not by default.

## Skill inventory

All 15 skills, paths relative to this file:

| Path | Purpose |
|---|---|
| `dotpudica-route/SKILL.md` | This skill: routing entry point, decision table + invocation order |
| `capabilities/dotpudica-bootstrap/SKILL.md` | Project integration, csproj injection check, AppContext bootstrapping |
| `capabilities/dotpudica-view/SKILL.md` | View + ViewModel + declarative bindings (BindTo / BindCommand / ItemsSource) |
| `capabilities/dotpudica-di-scope/SKILL.md` | Service registration, [Inject], ViewModelFactory, SceneContextHost |
| `capabilities/dotpudica-windows/SKILL.md` | GodotWindowManager / GodotWindow layering and Bundle data |
| `capabilities/dotpudica-pooling/SKILL.md` | NodePool, window pools, ItemsSource PoolSize, pooled views |
| `capabilities/dotpudica-messaging-threading/SKILL.md` | Messages, InteractionRequest, thread marshaling, cancellation |
| `capabilities/dotpudica-verify/SKILL.md` | Build verification, DOTPUDICA diagnostics, common pitfalls |
| `workflows/dotpudica-wf-project-setup/SKILL.md` | End-to-end: integrate DotPudica into a new Godot .NET project |
| `workflows/dotpudica-wf-add-page/SKILL.md` | End-to-end: deliver a complete new UI page |
| `workflows/dotpudica-wf-add-window/SKILL.md` | End-to-end: deliver a popup, dialog, or full-screen page |
| `workflows/dotpudica-wf-shared-data/SKILL.md` | End-to-end: share state between pages via singleton services |
| `workflows/dotpudica-wf-scene-scope/SKILL.md` | End-to-end: scene-isolated DI and leave-scene cancellation |
| `workflows/dotpudica-wf-pooling/SKILL.md` | End-to-end: optimize frequently opened panels and popups |
| `workflows/dotpudica-wf-fix-diagnostics/SKILL.md` | End-to-end: fix DOTPUDICA* compile diagnostics |

Tiers: 1 route + 7 capabilities (verify included) + 7 workflows = 15.
