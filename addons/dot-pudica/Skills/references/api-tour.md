# DotPudica API Tour — Key Types Quick Reference

Source of truth: addons/dot-pudica source code (same repo/version). Verify against source when in doubt.

Lookup table for the framework's key public types: the exact signatures and constraints of the bootstrap, DI/scene-scope, window, pooling, messaging, threading, and conversion APIs. The bootstrap / di-scope / windows / pooling / messaging-threading capability skills reference the matching section below on demand; every signature mirrors the source files listed per section. Read this before writing service registration, scene hosts, windows, pools, message flows, or value converters.

## 1. AppContext — application bootstrap

Source: `addons/dot-pudica/Godot/AppContext.cs` (`DotPudica.Godot`). Process-wide root context: framework initialization, IoC container, window manager, disposal. Initialize from a Godot Autoload (singleton node) or the main scene's `Node._Ready()`.

```csharp
public sealed class AppContext : IDisposable
{
    public static AppContext Current { get; }               // throws if not initialized
    public IServiceProvider Services { get; }               // root provider; scene scopes derive from it
    public GodotWindowManager WindowManager { get; }        // throws if windowManagerNode was not passed

    public AppContext Initialize(
        Action<IServiceCollection>? configureServices = null,
        GodotWindowManager? windowManagerNode = null);

    public void Dispose();
    internal static void ResetForUnload();                  // ALC unload only
}
```

Constraints:

- `Current` throws `InvalidOperationException` ("AppContext is not initialized, please call new AppContext().Initialize() first") until `Initialize()` completes. Every `Show`/`ShowPooled` and any `SceneContextHost` entering the tree depends on it.
- `Initialize` registers the `AppContext` itself as a singleton and (when `windowManagerNode != null`) the manager as `IWindowManager` singleton, then runs user `configureServices`.
- Calling `Initialize` a second time throws ("AppContext can only be initialized once until disposed or ALC unload resets it.").
- `Dispose` is idempotent: clears the window manager, nulls statics, resets `FrameworkRuntime`. A new `AppContext` can be initialized afterwards.
- `WindowManager` throws if `Initialize` was called without `windowManagerNode`.

Related skill: dotpudica-bootstrap (project integration, csproj injection check, AppContext bootstrapping).

## 2. SceneContextHost — per-scene DI + cancellation scope

Source: `addons/dot-pudica/Godot/SceneContextHost.cs` (`DotPudica.Godot`). Attach manually to a scene root; creates the scene DI scope and the scene operation scope on entering the tree and disposes them on leaving.

```csharp
public partial class SceneContextHost : Node
{
    public ISceneScope Scope { get; }             // throws before entering the tree
    public SceneOperationScope Operations { get; } // throws before entering the tree
}
```

Constraints:

- `_EnterTree` builds the scope from `AppContext.Current.Services` (via `SceneScope.Create`) and creates `Operations`; requires `AppContext.Initialize` to have finished.
- `_ExitTree` disposes `Operations` first, then `Scope` — Godot guarantees child `_ExitTree` runs before parent `_ExitTree`, so views release before the scope goes away.
- Accessing `Scope` / `Operations` before the node entered the tree throws `InvalidOperationException`.

Related skill: dotpudica-di-scope (service registration, `[Inject]`, ViewModelFactory, SceneContextHost).

## 3. GodotWindowManager — stack policy, queued popups, window pools

Source: `addons/dot-pudica/Godot/Views/GodotWindowManager.cs` + `WindowTypes.cs` (`DotPudica.Godot.Views`). Owns the window stack, QueuedPopup FIFO, Full-page passivation/restore, and per-type window pools.

```csharp
public partial class GodotWindowManager : Node, IWindowManager
{
    public event EventHandler? StackChanged;
    public IWindow? Current { get; }              // stack top (last entry)
    public IReadOnlyList<IWindow> Stack { get; }  // snapshot copy — safe to dismiss while enumerating
    public int QueuedCount { get; }

    public ITransition Show(IWindow window, bool ignoreAnimation = false);
    public ITransition Hide(IWindow window, bool ignoreAnimation = false);   // forwards to IWindow.Hide
    public ITransition Dismiss(IWindow window, bool ignoreAnimation = false); // forwards to IWindow.Dismiss
    public T? Find<T>() where T : class, IWindow;                             // top-most match

    public void ConfigurePool<TWindow>(int maxSize) where TWindow : GodotWindow, new();
    public TWindow ShowPooled<TWindow>(IBundle? bundle = null, bool ignoreAnimation = false)
        where TWindow : GodotWindow, new();
    public void Clear(Func<IWindow, bool>? predicate = null);
}
```

`Show` strategy by `WindowType` (only `Full` and `QueuedPopup` branch; `Popup`/`Dialog`/`Progress` are plain overlay labels):

- `QueuedPopup`: enqueued (returns `CompletedTransition.Instance`) while another QueuedPopup is visible, or while the stack top is still a QueuedPopup including hidden (preserves FIFO after `Hide`). The next queued popup shows after the current one dismisses.
- `Full`: passivates the previous window — `Dismiss(ignoreAnimation: true)` if it is mid-dismiss, else `Hide(true)`. When a Full window is dismissed/forgotten, the previous Full (if any) is shown again.
- Orphan fallback: a window node without a parent is `AddChild`-ed (hosts should parent via `Prepare*` before `Show`).

Constraints:

- `ConfigurePool<TWindow>(maxSize)` must be called before `ShowPooled<TWindow>()` — `ShowPooled` throws "No window pool configured for {T}; call ConfigurePool first." `ConfigurePool` is idempotent for the same maxSize and throws `InvalidOperationException` for a different one.
- `Clear(predicate)` dismisses matching windows and releases matching queued popups; a null predicate clears everything and disposes all pool entries.
- Pooled windows recycle on dismiss (detach → reset → cache); they are `QueueFree`-ed when the pool is gone or full. Pool entries are disposed on `Clear(null)` and `_ExitTree`.

Related skill: dotpudica-windows (GodotWindowManager / GodotWindow layering and Bundle data).

## 4. GodotWindow — window base class

Source: `addons/dot-pudica/Godot/Views/GodotWindow.cs` + `WindowTypes.cs` (`DotPudica.Godot.Views`). Adds window lifecycle management on top of `Control`: `Create → Show → Activate → Passivate → Hide → Dismiss`.

```csharp
public abstract partial class GodotWindow : Control, IWindow
{
    public event EventHandler? WindowVisibilityChanged;
    public event EventHandler? WindowActivationChanged;
    public event EventHandler? WindowDismissed;
    public event EventHandler<WindowStateEventArgs>? StateChanged;

    public WindowType WindowType { get; set; }  // default Full; enum: Full/Popup/Dialog/Progress/QueuedPopup
    public string WindowName { get; set; }
    public bool Created { get; }
    public bool Dismissed { get; }
    public bool IsDismissing { get; }           // dismiss transition in flight
    public bool IsWindowVisible { get; }
    public bool IsWindowActivated { get; }
    public WindowState State { get; }           // Begin/End markers fire synchronously inside one transition callback

    public void Create(IBundle? bundle = null); // no-op when already Created; calls OnCreate(bundle)
    public ITransition Show(bool ignoreAnimation = false);
    public ITransition Hide(bool ignoreAnimation = false);
    public ITransition Dismiss(bool ignoreAnimation = false);

    protected virtual void OnCreate(IBundle? bundle) { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    protected virtual void OnDismiss() { }
}
```

Constraints:

- `Show`/`Hide`/`Dismiss` throw `InvalidOperationException` ("Cannot dismiss a dismissed window.") once `Dismissed` is true — a dismissed window is terminal.
- `Dismiss` repeated while already dismissing reuses the in-flight transition instead of canceling it (so `WindowDismissed` still fires).
- Non-pooled `Dismiss` ends with `QueueFree()`; pooled windows (`IsPooled` set by `ShowPooled`) are recycled by the manager instead.
- `_ExitTree` cancels the active transition and notifies the manager via `Forget(this)`.
- `WindowState` markers (`CreateBegin/End`, `Visible`, `Activated`, `DismissBegin/End`, …) are raised synchronously inside the transition callbacks — no separate sub-animations.

Related skill: dotpudica-windows (layering, transitions, Bundle payloads).

## 5. NodePool / NodeFactory / SceneFactory — Godot-side pooling

Source: `addons/dot-pudica/Godot/ObjectPool/NodePool.cs` (`DotPudica.Godot.ObjectPool`). Thin Node/Scene facade over Core's `ObjectPool<T>`; the pool algorithm lives in Core, these types only handle creation, tree removal, validation, and QueueFree.

```csharp
public static class NodePool
{
    public static IObjectPool<Node> Create(PackedScene scene, int maxSize);         // throws if maxSize <= 0
    public static IObjectPool<T> Create<T>(int maxSize = 0) where T : Node, new(); // new T()
    public static IObjectPool<T> Create<T>(PackedScene scene, int maxSize = 0) where T : Node;
    public static IObjectPool<T> Create<T>(string scenePath, int maxSize = 0) where T : Node; // GD.Load
}

public class NodeFactory<T> : IObjectFactory<T> where T : Node, new();   // Create = new T()
public class SceneFactory<T> : IObjectFactory<T> where T : Node;         // Create = scene.Instantiate<T>()
```

Constraints:

- Only the non-generic `Create(PackedScene, int maxSize)` overload validates `maxSize > 0` (`ArgumentOutOfRangeException`). The generic overloads accept `maxSize = 0`, which Core's `ObjectPool<T>` rewrites to `Environment.ProcessorCount * 2`.
- Factory `Reset` removes the node from its parent; `Validate` is `GodotObject.IsInstanceValid`; `Destroy` is `QueueFree`.
- `SceneFactory(string)` throws `ArgumentException` when the scene cannot be loaded; the `PackedScene` ctor throws on null.

Related skill: dotpudica-pooling (NodePool, window pool, ItemsSource PoolSize, pooled views).

## 6. ObjectPool\<T> — Core pool algorithm

Source: `addons/dot-pudica/Core/ObjectPool/ObjectPool.cs` + `IObjectPool.cs` + `IObjectFactory.cs` (`DotPudica.Core.ObjectPool`). Thread-safe, lock-free generic pool (Interlocked.CompareExchange per slot).

```csharp
public class ObjectPool<T> : IObjectPool<T>, IObjectPool where T : class
{
    public ObjectPool(IObjectFactory<T> factory, int initialSize = 0, int maxSize = 0);
    public int MaxSize { get; }
    public int InitialSize { get; }
    public T Allocate();
    public void Free(T obj);
    public void Dispose();  // destroys all pooled objects
}

public interface IObjectPool<T> : IDisposable { T Allocate(); void Free(T obj); int MaxSize { get; } }
public interface IObjectFactory<T> { T Create(IObjectPool<T> pool); void Reset(T obj); bool Validate(T obj); void Destroy(T obj); }
```

Constraints:

- `maxSize <= 0` in the constructor is replaced with `Environment.ProcessorCount * 2`; `maxSize < initialSize` throws `ArgumentException`.
- `Allocate` reuses a free slot or falls back to `factory.Create`; throws `ObjectDisposedException` after `Dispose`.
- `Free`: null is a no-op; if disposed or `Validate` fails → `factory.Destroy`; otherwise `Reset` then store; when the pool is full → `Destroy` (overflow objects are destroyed, never queued).
- `Dispose` clears and destroys every pooled object.

Related skill: dotpudica-pooling (pool sizing, overflow behavior, disposal).

## 7. MessageBus — messaging facade

Source: `addons/dot-pudica/Core/Messaging/MessageBus.cs` (`DotPudica.Core.Messaging`). Static facade over CommunityToolkit.Mvvm's Messenger; weak-reference (auto-unbind) and strong-reference modes.

```csharp
public static class MessageBus
{
    public static IMessenger Default { get; }   // WeakReferenceMessenger.Default (recommended)
    public static IMessenger Strong { get; }    // StrongReferenceMessenger.Default (manual Unregister required)

    public static TMessage Send<TMessage>(TMessage message) where TMessage : class;
    public static TMessage Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class where TToken : IEquatable<TToken>;   // channel-keyed

    public static void Register<TRecipient, TMessage>(TRecipient recipient,
        MessageHandler<TRecipient, TMessage> handler) where TRecipient : class where TMessage : class;

    public static void UnregisterAll(object recipient);       // weak bus
    public static void UnregisterAllStrong(object recipient); // strong bus
    public static void Reset();                               // clears both busses (ALC unload)
}
```

Constraints:

- `Send`/`Register`/`UnregisterAll` shortcut methods always target the weak-reference bus; the strong bus is reached via `Strong` or `UnregisterAllStrong`.
- Weak mode is recommended (GC auto-unbinds); strong mode must be manually unregistered or it leaks.
- `Reset` clears both busses so ALC unload is not blocked by leftover handlers.

Related skill: dotpudica-messaging-threading (messages, thread marshaling, cancellation).

## 8. InteractionRequest\<T> — ViewModel → View interaction

Source: `addons/dot-pudica/Core/Interactivity/` (`InteractionRequestOfT.cs`, `InteractionEventArgs.cs`, `InteractionRequest.cs`). The ViewModel raises a request; the View layer subscribes and performs the UI-only operation, then invokes the optional callback.

```csharp
public sealed class InteractionRequest<T>
{
    public event EventHandler<InteractionEventArgs<T>>? Raised;

    public void Raise(T context);
    public void Raise(T context, Action<T>? callback);
}

public sealed class InteractionEventArgs<T> : EventArgs
{
    public T Context { get; }
    public Action? Callback { get; }  // null when no callback was passed; invoke after the interaction completes
}
```

Constraints:

- `Raise` with no subscribers is a silent no-op (the null handler is returned early).
- The `callback` closes over `context` and is wrapped as a parameterless `Action`; the View should call `args.Callback` after the interaction ends — this lets the View notify the VM without the View calling VM methods directly.
- A non-generic `InteractionRequest` (`event EventHandler? Raised; void Raise();`) exists for context-free requests.

Related skill: dotpudica-messaging-threading (InteractionRequest pattern, no-subscriber behavior).

## 9. IUiDispatcher / UiDispatcher — thread marshaling

Source: `addons/dot-pudica/Core/Binding/BindingContext.cs` (`DotPudica.Core.Binding`). Core bindings depend only on this small contract, so UI objects stay on their required thread.

```csharp
public interface IUiDispatcher
{
    bool CheckAccess();
    void Post(Action action);
}

public static class UiDispatcher
{
    public static IUiDispatcher Immediate { get; }                                  // synchronous, CheckAccess always true
    public static IUiDispatcher FromSynchronizationContext(SynchronizationContext context);
    public static IUiDispatcher CaptureCurrentOrImmediate();                        // SynchronizationContext.Current or Immediate
}
```

Constraints:

- `Immediate` executes posted actions synchronously (headless tests, no SyncContext).
- `SynchronizationContextUiDispatcher.Post` runs inline when already on the context, else `context.Post`.
- `BindingContext.SetUiDispatcher(dispatcher)` must be called before any binding is created — otherwise it throws ("The UI dispatcher must be set before creating bindings."). Binding lifecycle operations throw when off the UI thread.

Related skill: dotpudica-messaging-threading (marshal results back to the UI thread).

## 10. LatestSnapshotMailbox\<T> — background → main snapshot handoff

Source: `addons/dot-pudica/Core/Threading/LatestSnapshotMailbox.cs` (`DotPudica.Core.Threading`). Background threads publish only the last immutable snapshot; the main thread drains once on demand and applies it to the ViewModel.

```csharp
public sealed class LatestSnapshotMailbox<T>
{
    public void Publish(T immutableSnapshot);   // throws ArgumentNullException for null reference snapshots
    public bool TryDrainLatest(out T? snapshot); // take-and-clear; false when empty
}
```

Constraints:

- Latest-wins: each `Publish` overwrites the previous snapshot; intermediate states are dropped.
- Do not mutate `ObservableCollection`s bound to ItemsSource item-by-item from a background thread — publish an immutable snapshot and drain it on the main thread.

Related skill: dotpudica-messaging-threading (network callbacks, snapshot UI updates).

## 11. SceneOperationScope — scene-level cancellation

Source: `addons/dot-pudica/Core/Threading/SceneOperationScope.cs` (`DotPudica.Core.Threading`). Cancel when leaving a scene, disconnecting, or leaving a room, so in-flight network requests stop or stop updating the UI.

```csharp
public sealed class SceneOperationScope : IDisposable
{
    public CancellationToken Token { get; }   // throws ObjectDisposedException after Dispose

    public CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] additionalTokens);
    public void Cancel();                     // no-op after Dispose
    public void Dispose();                    // Cancel + dispose CTS
}
```

Constraints:

- Use `CreateLinkedTokenSource()` for individual match/reconnect operations — cancelling the child token does not cancel the whole scene.
- `Token` access after `Dispose` throws `ObjectDisposedException`; `Cancel` after dispose is a silent no-op.
- `SceneContextHost.Operations` exposes the scope for the current scene; the host disposes it on `_ExitTree`.

Related skill: dotpudica-messaging-threading (leave-scene cancellation) and dotpudica-di-scope (SceneContextHost wiring).

## 12. ViewModelBase — base ViewModel

Source: `addons/dot-pudica/Core/ViewModels/ViewModelBase.cs` (`DotPudica.Core.ViewModels`). Inherits CommunityToolkit's `ObservableObject` and integrates the message bus and logging.

```csharp
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    protected ILog Log { get; }                             // lazy, named by type
    protected IMessenger Messenger { get; }                 // WeakReferenceMessenger.Default
    protected void Send<TMessage>(TMessage message) where TMessage : class;
    protected void Register<TMessage>(MessageHandler<ViewModelBase, TMessage> handler) where TMessage : class;

    public bool IsDisposed { get; }
    protected virtual void OnDispose() { }                  // subclass cleanup hook
    public void Dispose();                                  // UnregisterAll (both busses) + OnDispose + IsDisposed
}
```

Constraints:

- `Register` uses the weak messenger — GC auto-unbinds, no manual unregister required (but `Dispose` still unregisters from both the weak and the strong busses to avoid blocking ALC unload).
- Developer ViewModels inherit directly from `ViewModelBase`; CommunityToolkit source generators (`[ObservableProperty]`, `[RelayCommand]`) work on derived classes.

Related skill: dotpudica-view (View + ViewModel + bindings).

## 13. IValueConverter and built-in converters

Source: `addons/dot-pudica/Core/Binding/IValueConverter.cs`, `ConverterRegistry.cs`, `Converters/BuiltInConverters.cs` (`DotPudica.Core.Binding`). Strongly-typed converters for zero-allocation binding; a type-erased interface is reserved for object pipelines.

```csharp
public interface IValueConverter<TIn, TOut>
{
    TOut Convert(TIn value);
    TIn ConvertBack(TOut value);
}

public interface IValueConverter
{
    object? Convert(object? value, Type targetType);
    object? ConvertBack(object? value, Type targetType);
}

public static class ConverterRegistry
{
    public static void Register<TConverter>(TConverter converter) where TConverter : class;
    public static bool TryGetTyped<TIn, TOut>(Type converterType, out IValueConverter<TIn, TOut>? converter);
    public static bool TryGet(Type converterType, out IValueConverter? converter);
    public static void Clear();
}
```

Six built-in converters in `DotPudica.Core.Binding.Converters` — each implements both interfaces and exposes a `static readonly Instance` singleton (generated code references the singletons directly to avoid reflection activation):

| Converter | Conversion | Notes |
|---|---|---|
| `BoolNegateConverter` | `bool` ↔ `!bool` | |
| `BoolToVisibilityConverter` | `bool` → `bool` | Pass-through for Godot `Visible` targets; named for declarative clarity |
| `IntToStringConverter` | `int` ↔ `string` | `ToString` with `CurrentCulture`; `ConvertBack` parses or returns 0 |
| `FloatToStringConverter` | `float` ↔ `string` | Fixed `"F2"` format; `ConvertBack` parses or returns 0f |
| `ObjectToStringConverter` | `object` → `string` | `ToString()` or `""`; `ConvertBack` passes the value through |
| `StringToBoolConverter` | `string` → `bool` | Non-null and non-whitespace = true; `ConvertBack` returns `value.ToString()` |

Constraints:

- A binding with mismatched source/target types requires the typed `IValueConverter<TSource,TTarget>` for that exact pair — see `DOTPUDICA032`/`DOTPUDICA033` (references/diagnostics.md).
- Converters are stateless by design; reuse the `Instance` singleton rather than allocating.

Related skill: dotpudica-view (converter usage in `[BindTo]` / `[BindCommand]`) and dotpudica-verify (DOTPUDICA032–035 converter pitfalls).
