---
name: dotpudica-windows
description: Build window stacks and popups with DotPudica: GodotWindowManager node placement, GodotWindow subclasses, WindowType selection (Full/Popup/QueuedPopup/Dialog/Progress), Show/Hide/Dismiss calls, and Bundle data passing. Use whenever the user wants full-screen page switching, popups, dialogs, confirm boxes, or stacked UI layers.
---

# dotpudica-windows

## Purpose

Build and manage layered UI with DotPudica: full-screen page stacks, popups, dialogs, and queued overlays. A `GodotWindow` subclass is shown through the `GodotWindowManager` (or directly via `Show`/`ShowPooled`), and per-window data travels through a `Bundle` delivered to `OnCreate(IBundle?)`. The manager owns stack policy (Full mutual exclusion, QueuedPopup FIFO), transitions, and per-type window pools.

## Input Contract

Preconditions:

- The bootstrap is complete: `AppContext.Initialize` received the manager node via `GetNodeOrNull<GodotWindowManager>("WindowManager")` (passing `null` is legal but `AppContext.WindowManager` / `IWindowManager` resolution then throws).
- The scene contains exactly one manager node participating in window management.

Valid input states:

- A window class = `GodotWindow` subclass + `[DotPudicaView(typeof(TVM))]`, with `_Ready() => InitializeView()` / `_ExitTree() => DisposeView()` declared (missing them is `DOTPUDICA046`).
- `WindowType` chosen by semantics: `Full` = mutually exclusive full-screen page; `Popup` = overlay; `QueuedPopup` = queued overlay (enters the FIFO while another QueuedPopup is visible); `Dialog`/`Progress` = semantic overlays (same stack policy as `Popup`).
- The window being shown has not been dismissed before — a dismissed window is terminal.

## Procedure

1. Confirm the manager node exists and was passed to `Initialize`. Checkpoint: `Initialize` must have completed before any `Show`/`ShowPooled` call.
2. Write the window class (two lifecycle lines + optional `OnCreate(IBundle? bundle)` hook):

```csharp
[DotPudicaView(typeof(ConfirmViewModel))]
public partial class ConfirmDialog : GodotWindow
{
    public override void _Ready() => InitializeView();
    public override void _ExitTree() => DisposeView();

    protected override void OnCreate(IBundle? bundle) { }
}
```

3. Show and dismiss from the call site. `Show` takes no bundle — pass the `Bundle` via `Create(IBundle?)` (non-pooled) or `ShowPooled<T>(IBundle?)` (pooled):

```csharp
var dialog = new ConfirmDialog();
dialog.Create(new Bundle().Set("title", "Delete?"));
wm.Show(dialog);

// On the View side, get the manager via [Inject] IWindowManager or AppContext.Current.WindowManager
wm.Dismiss(dialog);
```

4. Checkpoints:

- `Full` windows are mutually exclusive: showing a new Full first hides (or, if mid-dismiss, dismisses without animation) the previous Full; dismissing the top Full shows the previous Full again.
- QueuedPopup is FIFO: a QueuedPopup is enqueued while another is visible or while the stack top is still a QueuedPopup (including hidden); the next one shows after the current one dismisses.
- `Dismiss` on an already-dismissed window throws `InvalidOperationException`.
- A non-pooled window ends its `Dismiss` with automatic `QueueFree()`; pooled windows (via `ShowPooled<T>`) are recycled by the manager instead.
- A window node without a parent is `AddChild`-ed as a fallback — host window nodes via `Prepare*` before `Show` when parenting matters.

## Output Contract

Deliverable = the window class + call-site code (create-with-bundle, show, dismiss) +, for cross-page switching, the navigation calls (dismiss current Full / show next Full). Acceptance:

- Build with zero DOTPUDICA diagnostics (look up any in `references/diagnostics.md`).
- Runtime checkpoints: open/close transitions complete, the stack top is the window you expect, the Bundle data reaches `OnCreate(IBundle?)`, and no leak — after closing, the scene tree has no residue of the dismissed window.

## Failure Handling

| Symptom | Fix action |
|---|---|
| `Dismiss` on an already-dismissed window throws `InvalidOperationException` ("Cannot dismiss a dismissed window.") | Check `Dismissed` or subscribe to `WindowDismissed` before dismissing; a dismissed window is terminal and must not be reused. |
| `AppContext.WindowManager` access throws ("WindowManager is not configured...") | `Initialize` was called without the manager node — pass the `GodotWindowManager` node into `Initialize`. |
| A window does not show | Wrong `WindowType` for the intent (e.g. a Full page hidden by another Full, or a QueuedPopup stuck behind a visible one) — verify stack mutual-exclusion semantics and `IsWindowVisible`. |
| `Show` before `Initialize` completed (AppContext exceptions) | Sequencing discipline: bootstrap `Initialize` before any window operation. |

## References

- `references/api-tour.md` — section 3 (GodotWindowManager: stack policy, QueuedPopup FIFO, pools) and section 4 (GodotWindow: Create/Show/Hide/Dismiss lifecycle, OnCreate hook, events).
- `references/lifecycle.md` — `InitializeView`/`DisposeView` step order and hook timing for `[DotPudicaView]` windows.
- Related skills: `dotpudica-bootstrap` (manager node wiring into `Initialize`), `dotpudica-pooling` (window pools, `ConfigurePool` before `ShowPooled`), `dotpudica-route` (navigation flows built on window stacks).
