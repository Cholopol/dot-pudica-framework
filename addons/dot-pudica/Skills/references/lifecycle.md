# DotPudica View Lifecycle Reference

Source of truth: addons/dot-pudica source code (same repo/version). Verify against source when in doubt.

Lookup table for the view lifecycle: the exact step order of `InitializeView` / `DisposeView` / `RecycleView` / `ActivateViewModel` as emitted by the source generator, the `AutoInitialize` x `Pooled` mode matrix, and hook timing. Every step below mirrors `BindingGenerator.cs` (`AppendInitializeViewCore` / `AppendLifecycleMembers` / `AppendDisposeView` / `AppendRecycleViewMembers` / `AppendPooledViewMembers`) and `DotPudicaViewRuntime.cs` (`SetViewModel` / `Recycle` / `Dispose` / `CaptureUiContext`). Read this before writing any view, pool, or shared-panel code.

## 1. Lifecycle methods

### 1.1 InitializeView (AutoInitialize = true)

Emitted by `AppendLifecycleMembers`. Executes in this exact order:

1. `[Inject]` service assignment — each writable `[Inject]` member is resolved from `AppContext.Current.Services` and assigned.
2. `OnViewReady()` — ViewModel does not exist yet; build the UI here.
3. `SetViewModel(CreateViewModel(), Ownership)` — VM created via `CreateViewModel()` (user `[ViewModelFactory]` method if declared, else `new`), ownership from the attribute (`Owned` default, or the declared `Ownership`).
4. `DotPudicaInitialize()` — runs `CaptureUiContext()` (validates the Godot main thread: throws "DotPudica bindings must be initialized on the Godot main thread" if called off-thread; idempotent for reuse) then `__DotPudicaInitializeBindingsCore()` (executes all declared bindings: `[BindTo]` / `[BindCommand]` / `[ItemsSource]`).
5. `[Subscribe]` — each `[Subscribe]` handler is attached to the ViewModel event path.
6. `OnViewModelBound()` — ViewModel is bound and initialized; navigate, start services here.

### 1.2 DisposeView

Emitted by `AppendDisposeView` (all modes). Executes in this exact order:

1. `OnViewDisposing()` — ViewModel is still accessible; cancel scopes, manual cleanup.
2. `[Subscribe]` unsubscribe — handlers detached (guarded by `if (ViewModel is { } __vm)`).
3. `DotPudicaDispose()` — runtime `Dispose()`: `BindingContext.Dispose()` (releases all bindings) + ViewModel lease released (`Owned` disposes the VM, `External` drops the reference).

### 1.3 RecycleView (Pooled only)

Emitted by `AppendRecycleViewMembers`. Executes in this exact order:

1. `OnViewDisposing()` — same as 1.2 step 1.
2. `[Subscribe]` unsubscribe.
3. Runtime `Recycle()` — `BindingContext.ClearBindings()` + ViewModel lease released (`External` drops the reference / `Owned` disposes the VM). The node stays alive and is NOT freed.
4. `RequestReady()` — re-arms the node: Godot calls `_ready()` once per node instance, so after re-parenting the node re-runs `_Ready() => InitializeView()` on the next tree entry.

### 1.4 ActivateViewModel(viewModel) (Pooled + AutoInitialize = false)

Emitted by `AppendPooledViewMembers`. Executes in this exact order:

1. `SetViewModel(viewModel, ViewModelOwnership.External)` — ownership is always `External`: the VM is owned by the caller.
2. `DotPudicaInitialize()` — main-thread validation + all declared bindings.
3. `[Subscribe]` — handlers attached.
4. `OnViewModelBound()`.

Used for shared-VM rebinding: the pooled view exposes a public `BindShared(vm) => ActivateViewModel(vm)` method and re-activates itself with a new ViewModel each time it is pooled out.

## 2. Mode matrix (AutoInitialize x Pooled)

| AutoInitialize | Pooled | `_Ready()` | `_ExitTree()` | Lifecycle |
|---|---|---|---|---|
| true | false | `InitializeView()` | `DisposeView()` | Full automatic lifecycle (1.1 + 1.2). VM created and owned by the view; destroyed on exit. |
| true | true | `InitializeView()` | `RecycleView()` | Full automatic lifecycle + recycling (1.1 + 1.3). A fresh `Owned` VM is created on every re-entry (each `_Ready` calls `CreateViewModel` again). |
| false | false | `InitializeView()` | `DisposeView()` | `InitializeView` only performs injection + `OnViewReady` (no VM creation, no bindings). User manually calls `SetViewModel(vm, ViewModelOwnership.External)` + `DotPudicaInitialize()`. Teardown still fully emitted. |
| false | true | `InitializeView()` | `RecycleView()` | Same as above, plus `ActivateViewModel(vm)` / `RecycleView()` for shared-VM pooling (1.3 + 1.4). |

Entry points (Godot only dispatches user-declared overrides; `_Ready` / `_ExitTree` must be written by the user — missing them is `DOTPUDICA046`):

```csharp
public override void _Ready() => InitializeView();
public override void _ExitTree() => DisposeView();   // or RecycleView() when Pooled
```

## 3. Hook timing

| Hook | When it runs | What is valid |
|---|---|---|
| `OnViewReady()` | Inside `InitializeView`, before VM creation (step 2 of 1.1) | ViewModel does not exist; build the UI tree, static visuals |
| `OnViewModelBound()` | After bindings + subscriptions (step 6 of 1.1 / step 4 of 1.4) | ViewModel is bound and live; navigate, start services, read VM state |
| `OnViewDisposing()` | First step of teardown (1.2 / 1.3) | ViewModel still accessible; cancel scopes, manual cleanup |

## 4. Sequencing reminders

- `AppContext.Current` throws "AppContext is not initialized, please call new AppContext().Initialize() first" unless `Initialize()` has completed. Any `Show` / `ShowPooled` call and any `SceneContextHost` entering the tree require `AppContext.Initialize` to have finished first.
- `ConfigurePool<TWindow>(maxSize)` must be called before `ShowPooled<TWindow>()` — `ShowPooled` throws "No window pool configured for ...; call ConfigurePool first." (`GodotWindowManager`).
