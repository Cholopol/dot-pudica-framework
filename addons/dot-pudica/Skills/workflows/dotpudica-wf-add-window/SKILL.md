---
name: dotpudica-wf-add-window
description: End-to-end workflow: deliver a popup, dialog, or full-screen page with the DotPudica window manager - window class, WindowType decision, Show/Dismiss calls, Bundle data passing, and verification. Use when the user wants a popup, confirm dialog, notification, or screen switching.
---

# dotpudica-wf-add-window

## Purpose

Take a described popup, dialog, notification, or full-screen page and deliver it end to end: a `GodotWindow` subclass with the two lifecycle lines, a `WindowType` decision, `Show`/`Dismiss` calls with a `Bundle` payload, and verification through build and runtime. This workflow owns the end-to-end task graph; `capabilities/dotpudica-windows` owns the "how" of each node. It covers the window-stack side of the framework — everything layered through the `GodotWindowManager`.

## Trigger Patterns

Use this workflow when the user says:

- "Show a popup" / "confirm dialog" / "notification toast" / "loading indicator".
- "Full-screen page switch" / "screen A to screen B" — any navigation where the previous view must close and the next opens.
- An existing window is built without the manager — raw `AddChild`/`QueueFree` plumbing instead of `Show`/`Dismiss` and stack policy.

Do not use it for a page that lives inside an existing window or scene (route to `dotpudica-wf-add-page`) or for projects with no bootstrap (route to `dotpudica-wf-project-setup` first).

## Task graph

Execute the nodes in order; close each node's acceptance point before advancing.

- **N1 — Clarify the popup** (deliverable: requirement summary; invokes no skill): the window kind (modal confirm, informational popup, full-screen page), the trigger entry point (button, event, route), and the data flowing in and out (which `Bundle` keys it reads, what it reports back). Acceptance: the summary names the window kind, its trigger, and its data in/out — no open questions.
- **N2 — Verify the manager** (deliverable: readiness confirmed; invokes `capabilities/dotpudica-bootstrap` for the check): the scene contains a `GodotWindowManager` node and the bootstrap `Initialize` call passes it (via `GetNodeOrNull<GodotWindowManager>("WindowManager")`). If missing, run the bootstrap fix first. Acceptance: the manager node exists, `Initialize` is called with it, and it completes before any `Show` call.
- **N3 — Decide the WindowType** (deliverable: type conclusion; invokes `capabilities/dotpudica-windows`): `Full` = mutually exclusive full-screen page; `Popup` = overlay; `QueuedPopup` = queued overlay (FIFO while another is visible); `Dialog`/`Progress` = semantic overlays. Acceptance: the chosen type matches the N1 summary and the intent, and the stack-policy consequence is understood.
- **N4 — Write the window class** (deliverable: window class; invokes `capabilities/dotpudica-windows`): a `GodotWindow` subclass with `[DotPudicaView(typeof(TVM))]`, the two lifecycle lines `_Ready() => InitializeView()` / `_ExitTree() => DisposeView()`, and an optional `OnCreate(IBundle?)` hook reading the Bundle. Acceptance: the class builds with no DOTPUDICA diagnostics.
- **N5 — Wire the call points** (deliverable: call-site code; invokes `capabilities/dotpudica-windows`): create the window, pass the `Bundle` via `Create(IBundle?)` or `ShowPooled<T>(IBundle?)`, call `Show`, and `Dismiss` from the View side (`[Inject] IWindowManager` or `AppContext.Current.WindowManager`). Acceptance: `Initialize` is complete before `Show`; `Dismiss` is not called on an already-dismissed window.
- **N6 — Build and runtime check** (deliverable: build result; invokes `capabilities/dotpudica-verify`): `dotnet build` with zero diagnostics, then launch and exercise open/close. Acceptance: open/close transitions complete, the stack top is the expected window, Bundle data reaches `OnCreate(IBundle?)`, and closing leaves no residue of the window in the scene tree.

```powershell
Select-String "WindowManager" <bootstrap-class>.cs
dotnet build
```

## Acceptance criteria

The workflow is complete when all of these hold:

- `dotnet build` completes with zero DOTPUDICA diagnostics.
- The window opens and closes per its `WindowType` semantics.
- `Full` windows are mutually exclusive — opening a new one hides the previous; dismissing the top restores it.
- After `Dismiss`, the scene tree holds no residue of the dismissed window (no leak).

## Failure branches

| Node | Symptom | Fix |
|---|---|---|
| N2 | `AppContext.WindowManager` access throws ("WindowManager is not configured...") | The manager node is missing or not passed to `Initialize` — use `capabilities/dotpudica-bootstrap` to add the node and pass it as a parameter; no window work before that. |
| N5 | `Dismiss` throws `InvalidOperationException` ("Cannot dismiss a dismissed window.") | The window was already dismissed and is terminal — guard with the `Dismissed` property (or `WindowDismissed` event) before dismissing. |
| N6 | Leak after closing — the dismissed window stays in the scene tree | Confirm a non-pooled window ends its `Dismiss` with automatic `QueueFree()`; do not manually `Free` an already-dismissed window. |

Any node checkpoint failure returns to the invoked capability skill to fix; never roll back and restart the graph from N1.

## Common variants

- **Confirm dialog with a callback**: the ViewModel drives the dialog via `InteractionRequest` and reacts to the user's choice through its `Raised` callback (`capabilities/dotpudica-messaging-threading`); the View subscribes and shows the window, then the callback carries the result back to the ViewModel.
- **Frequently opened popups**: route `Show` through the manager's pool — `ConfigurePool` before `ShowPooled<T>` (`capabilities/dotpudica-pooling`); pooled windows are recycled instead of `QueueFree`d.

## References

- `capabilities/dotpudica-windows` — N3 (WindowType semantics, stack policy), N4 (window class shape, `OnCreate` hook), N5 (`Create`/`Show`/`ShowPooled`/`Dismiss`, Bundle passing).
- `capabilities/dotpudica-bootstrap` — N2 (manager node wiring into `Initialize`).
- `capabilities/dotpudica-verify` — N6 (build command, diagnostic loop, runtime checklist).
- `capabilities/dotpudica-messaging-threading` — variant: `InteractionRequest<T>` driving the dialog from the ViewModel.
- `capabilities/dotpudica-pooling` — variant: window pools for frequently opened popups.
- `dotpudica-route` — related: the routing decision table routes "Popup / confirm dialog / full-screen page" to this workflow.
