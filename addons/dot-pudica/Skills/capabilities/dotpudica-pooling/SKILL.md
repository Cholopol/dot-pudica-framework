---
name: dotpudica-pooling
description: Add object pooling to DotPudica views, windows, and item lists: [DotPudicaView(Pooled = true)] with RecycleView, NodePool.Create/Allocate/Free, GodotWindowManager.ConfigurePool/ShowPooled, and [ItemsSource] PoolSize. Use when panels or popups are created and destroyed frequently, when item lists churn, or when the user reports stutter or garbage collection pressure from UI objects.
---

# dotpudica-pooling

## Purpose

Reuse DotPudica views, windows, and item rows across create/destroy cycles so frequently spawned UI stops churning allocations. Pooled views swap `DisposeView` for `RecycleView` (teardown without freeing the node, re-armed with `RequestReady`); pooled windows are recycled by `GodotWindowManager` on dismiss; item lists recycle rows through `[ItemsSource] PoolSize`. This is the I/O contract for pooling: when it is worth pooling, what a valid pooled declaration looks like, the exact borrow/return calls, and the failure modes (`DOTPUDICA045`, unconfigured or misconfigured pools).

## Input Contract

Preconditions:

- Decide whether pooling is worthwhile: pool panels, popups, and list rows that are created and destroyed frequently (stutter or GC pressure reported); a one-shot popup opened a handful of times does not need a pool — plain `DisposeView` + `QueueFree` is fine.
- A pooled node must be able to leave and re-enter the scene tree safely: `RecycleView` detaches bindings but keeps the node alive, and the next tree entry re-runs `_Ready() => InitializeView()`. Poolable controls must not hold scene-rooted assumptions (a pooled node has no parent while cached).
- Bootstrap is complete (`AppContext.Initialize` done); `ConfigurePool`/`ShowPooled` and any pooled view re-entry require it (see `references/lifecycle.md` section 4).

Valid input states:

- Pooled view = `[DotPudicaView(typeof(TVM), Pooled = true)]` + `_ExitTree() => RecycleView()` (missing the entry point is `DOTPUDICA046`).
- The holder owns the pool — `IObjectPool<T>` returned by `NodePool.Create<T>(maxSize)`.
- Window pools require `wm.ConfigurePool<T>(maxSize)` before the first `wm.ShowPooled<T>()`.
- `[ItemsSource]` `PoolSize > 0` is only valid on non-virtualized item targets; `VirtualizedItemsControl` manages its own recycling and rejects `PoolSize` with `DOTPUDICA045`.

## Procedure

1. Pick the pooling mode from the decision table:

| Target | Pattern |
|---|---|
| View with its own ViewModel, created fresh each time it is shown | `AutoInitialize = true` (default) + `Pooled = true`; each re-entry creates a new `Owned` VM |
| View reused with a caller-supplied ViewModel (multiple views bound to one shared instance) | `AutoInitialize = false` + `Pooled = true`; call `ActivateViewModel(vm)` each time the pooled view is allocated |
| Window | `Pooled = true` + `wm.ConfigurePool<T>(maxSize)` / `wm.ShowPooled<T>()` |
| Item list rows | `[ItemsSource] PoolSize = n` on the non-virtualized list; never set `PoolSize` on a `VirtualizedItemsControl` (triggers `DOTPUDICA045`) |

2. Pooled view example (`AutoInitialize = true` — the view is pooled, the VM is not):

```csharp
[DotPudicaView(typeof(ItemDetailViewModel), Pooled = true)]
public partial class ItemDetailPanel : VBoxContainer
{
    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();
}
```

3. Holder borrows and returns through the pool (remove from tree before `Free`, or the pool detaches it):

```csharp
var pool = NodePool.Create<ItemDetailPanel>(maxSize: 4);
var view = pool.Allocate();
host.AddChild(view);

view.GetParent()?.RemoveChild(view);
pool.Free(view);
```

4. Pooled window example:

```csharp
[DotPudicaView(typeof(PooledPopupViewModel), Pooled = true)]
public partial class PooledPopup : GodotWindow
{
    public override void _Ready() => InitializeView();
    public override void _ExitTree() => RecycleView();
}
```

5. Window pool is configured once, then shown/dismissed by the manager:

```csharp
wm.ConfigurePool<PooledPopup>(maxSize: 2);
wm.ShowPooled<PooledPopup>();
wm.Dismiss(window);
```

6. Contract points:

- Never `QueueFree` a pooled object directly — that leaves a stale cached node in the pool (validation is `IsInstanceValid`, so the pool would hand back a dead node). Always return through `pool.Free(view)` / `wm.Dismiss(window)`.
- Recycle does not destroy the node: it unbinds, releases the VM lease (`Owned` disposes the VM, `External` drops it), and `RequestReady()` re-arms it so the next tree entry re-runs `_Ready()`.
- A pooled node is `QueueFree`-ed as a fallback only when the pool is full or the pool itself is gone (window pools also dispose cached entries on `wm.Clear()` / `_ExitTree`).
- `wm.Clear()` with no predicate dismisses every window and destroys all pool caches.

## Output Contract

Deliverable = the pooled view/window declaration (`Pooled = true` + `RecycleView()`) plus the holder's borrow/return code. Acceptance:

- `dotnet build` passes with zero DOTPUDICA diagnostics.
- Runtime checkpoints: after opening and closing the same view N times in a row, the node instance count does not grow (the same instances are reused); every reactivation of a pooled window gets a fresh VM; in the shared-VM mode the VM survives the pooled view's recycle (ownership is `External`).

## Failure Handling

| Symptom (typical trigger) | See `references/diagnostics.md` entry / cause |
|---|---|
| `ShowPooled` throws "No window pool configured for ...; call ConfigurePool first." | `ConfigurePool<T>(maxSize)` was not called before `ShowPooled<T>` — call it once before the first show |
| `ConfigurePool` throws `InvalidOperationException` for a different maxSize | A type can only register one pool capacity; `ConfigurePool` is idempotent only for the same maxSize |
| `QueueFree` on a pooled object | Violates the contract — pooled cache retains a dead node; return via `pool.Free` / `wm.Dismiss` instead |
| `DOTPUDICA045` | `PoolSize` set on a `VirtualizedItemsControl` target — remove it; virtualized controls recycle their own rows |

## References

- `references/lifecycle.md` — `RecycleView` / `ActivateViewModel` step order, `AutoInitialize` x `Pooled` matrix, `RequestReady` re-arm.
- `references/api-tour.md` — `NodePool`/`ObjectPool` sections, `GodotWindowManager.ConfigurePool`/`ShowPooled`/`Clear`, window pool recycling rules.
- `references/diagnostics.md` — `DOTPUDICA045` (PoolSize on virtualized target) and the `DOTPUDICA046` lifecycle matrix.
- Related skills: `dotpudica-windows` (window stack policy and `Dismiss` semantics), `dotpudica-view` (view declaration, `AutoInitialize`, `[ItemsSource]`), `dotpudica-route` (choosing which skill a task needs).
