# DotPudica Skills

## What this is

An AI agent skill set for the **DotPudica** framework (Godot 4.7 + .NET 8 MVVM). It teaches an agent to write framework code for you — pages, bindings, windows, services, pooling, threading, and diagnostics — so the agent produces code that follows the framework's rules the first time. The skills cover the framework API only; they do not describe this repository's project layout or its example content.

## Skill inventory

15 skills in three tiers:

| Skill | Tier | Purpose |
|---|---|---|
| dotpudica-route | Routing | Single entry point for any framework task: decision table + invocation order |
| dotpudica-bootstrap | Capability | Project integration, csproj injection check, AppContext bootstrapping |
| dotpudica-view | Capability | View + ViewModel + bindings (BindTo / BindCommand / ItemsSource) |
| dotpudica-di-scope | Capability | Service registration, [Inject], ViewModelFactory, SceneContextHost |
| dotpudica-windows | Capability | GodotWindowManager / GodotWindow layering and Bundle data |
| dotpudica-pooling | Capability | NodePool, window pool, ItemsSource PoolSize, pooled views |
| dotpudica-messaging-threading | Capability | Messages, InteractionRequest, thread marshaling, cancellation |
| dotpudica-verify | Capability | Build verification, DOTPUDICA diagnostics, common pitfalls |
| dotpudica-wf-project-setup | Workflow | End-to-end: integrate DotPudica into a new Godot .NET project |
| dotpudica-wf-add-page | Workflow | End-to-end: deliver a complete new UI page |
| dotpudica-wf-add-window | Workflow | End-to-end: deliver a popup, dialog, or full-screen page |
| dotpudica-wf-shared-data | Workflow | End-to-end: share state between pages via singleton services |
| dotpudica-wf-scene-scope | Workflow | End-to-end: scene-isolated DI and leave-scene cancellation |
| dotpudica-wf-pooling | Workflow | End-to-end: optimize frequently opened panels and popups |
| dotpudica-wf-fix-diagnostics | Workflow | End-to-end: fix DOTPUDICA* compile diagnostics |

Capability skills are the I/O contract layer (Input Contract → Procedure → Output Contract → Failure Handling); workflow skills chain capabilities into end-to-end task graphs; the route skill picks between them.

## Installation

Three ways to make these skills available to an agent:

1. **Copy the skills into the agent's skills directory** (opencode example; other tools use their own directory):

```
Copy-Item -Recurse "addons/dot-pudica/Skills/*" "$env:USERPROFILE\.config\opencode\skills\"
```

2. **Point the agent at the directory in-repo**: tell the agent to read files under `addons/dot-pudica/Skills` (e.g. `C:\...\addons\dot-pudica\Skills`). The skills stay versioned with the addon and never drift.

3. **Register per agent-tool convention**: follow each tool's skills registration rules (for example, `.cursor/rules` for Cursor).

## How to use

Have the agent read `dotpudica-route/SKILL.md` first. The route skill decides which capabilities or workflows to invoke for the task at hand. Small single-capability tasks (one binding, one service, one event) map directly to the matching capability skill; multi-step requests route to a workflow skill. After any capability or workflow finishes, its Output Contract is the acceptance bar before moving on.

## Version anchor

This skill set lives in the same repository and is versioned together with the `addons/dot-pudica` source code. API facts stated here follow the source: when in doubt, verify against `addons/dot-pudica` in this repo.
