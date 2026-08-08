---
name: dotpudica-messaging-threading
description: Wire communication and threading with DotPudica: [Subscribe] to ViewModel events, MessageBus weak/strong messaging, InteractionRequest for ViewModel-to-View requests, IUiDispatcher.Post, LatestSnapshotMailbox, and SceneOperationScope cancellation. Use for cross-component events, async or network callbacks that touch the UI, 'notify me when X happens' requests, or cancelling work when leaving a scene.
---

# dotpudica-messaging-threading

## Purpose

Connect ViewModels, Views, and components through the DotPudica communication stack and marshal work across threads. This is the I/O contract for communication and threading: it defines the four communication shapes (ViewModel events via `[Subscribe]`, cross-component `MessageBus` messages, ViewModel-to-View `InteractionRequest<T>`, request-response messages), the thread discipline for touching the UI (main-thread controls, `IUiDispatcher.Post`, `LatestSnapshotMailbox<T>`), and scene-level cancellation with `SceneOperationScope`.

## Input Contract

Preconditions:

- `[Subscribe]` scenarios: the event source is on the ViewModel (a `public event` the VM raises), and the consuming View is a `[DotPudicaView]` partial class — the generator attaches subscriptions after bindings initialize and detaches them during teardown (see `references/lifecycle.md`).
- `MessageBus` scenarios: the message type is a plain class. Inheriting from `MessageBase` is optional; only the type identity matters.
- `SceneOperationScope` scenarios: the scope comes from a `SceneContextHost` (`host.Operations`) or a window-level scope — see `dotpudica-di-scope`.

Valid input states:

- ViewModel event = `event EventHandler` or an event with a parameter list; the `[Subscribe]` handler must return `void` and declare exactly the event's parameter count, with each handler parameter contravariant-compatible (it must accept every value the event delivers) — otherwise `DOTPUDICA042`.
- Cross-component notification (no direct reference between sender and receivers) → `MessageBus`, default weak-reference bus (GC auto-unbinds).
- The ViewModel wants to drive a UI-only View action (dialog, navigation) → `InteractionRequest<T>` — the VM stays Godot-free.
- One-shot request-response (a receiver must return a value) → `RequestMessage<TResponse>` / `AsyncRequestMessage<TResponse>`.

## Procedure

1. Pick the communication form:

| Need | Form |
|---|---|
| "Notify me when X happens" inside one VM's View pair | ViewModel `event` + `[Subscribe]` |
| Broadcast to any number of components (sender never references receivers) | `MessageBus` |
| ViewModel → View UI-only action (dialog, navigation, focus) | `InteractionRequest<T>` |
| One-shot request that returns a value to the sender | `RequestMessage<TResponse>` / `AsyncRequestMessage<TResponse>` |

2. Wire a ViewModel event with `[Subscribe]`. ViewModel first:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

public partial class MatchViewModel : ViewModelBase
{
    public event EventHandler? MatchSucceeded;

    [RelayCommand]
    private void FinishMatch()
    {
        MatchSucceeded?.Invoke(this, EventArgs.Empty);
    }
}
```

Then the View subscribes — the handler must match the delegate's parameter list exactly (void, `object? sender`, `EventArgs e`):

```csharp
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

[DotPudicaView(typeof(MatchViewModel))]
public partial class MatchView : Control
{
    public override void _Ready() => InitializeView();
    public override void _ExitTree() => DisposeView();

    [Subscribe("MatchSucceeded")]
    private void OnMatchSucceeded(object? sender, EventArgs e)
    {
        GD.Print("Match finished");
    }
}
```

Checkpoint: the event path must resolve on the ViewModel (chained paths work, e.g. `"CreateRequest.Raised"`); the handler is auto-attached after bindings and auto-detached during teardown — never unsubscribe by hand.

3. Broadcast with `MessageBus` — send from anywhere, receive without knowing the sender:

```csharp
public sealed class RoomStateChanged
{
    public string RoomId { get; init; } = "";
}

// sender — no reference to any receiver
MessageBus.Send(new RoomStateChanged { RoomId = "room-7" });

// receiver — register once; weak bus (default) auto-unbinds when this is GC'd
MessageBus.Register(this, static (RoomPanel receiver, RoomStateChanged message) =>
    receiver.OnRoomChanged(message.RoomId));

// strong bus only: the recipient is kept alive, so manual cleanup is required
MessageBus.Strong.Register(this, static (RoomPanel receiver, RoomStateChanged message) =>
    receiver.OnRoomChanged(message.RoomId));
MessageBus.UnregisterAllStrong(this);
```

Checkpoints: `MessageBus.Send` / `MessageBus.Register` / `MessageBus.UnregisterAll` always target the weak bus (`MessageBus.Strong` reaches the strong one); weak mode needs no manual unregister, strong mode leaks without `UnregisterAllStrong`.

4. Raise a ViewModel-to-View request with `InteractionRequest<T>`:

```csharp
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Interactivity;

public partial class LoadoutViewModel : ViewModelBase
{
    public InteractionRequest EnterMatchRequest { get; } = new();

    [RelayCommand]
    private void EnterMatch() => EnterMatchRequest.Raise();
}
```

The View subscribes to the request's `Raised` event and performs the UI action:

```csharp
[DotPudicaView(typeof(LoadoutViewModel))]
public partial class LoadoutPage : Control
{
    public override void _Ready() => InitializeView();
    public override void _ExitTree() => DisposeView();

    [Subscribe("EnterMatchRequest.Raised")]
    private void OnEnterMatchRequested(object? sender, EventArgs e)
    {
        RequireWindowManager().Show(new MatchPage { WindowName = "Match" });
    }
}
```

With a payload and a callback, the View reports back without calling VM methods:

```csharp
// ViewModel
public InteractionRequest<string> DeleteConfirmRequest { get; } = new();

[RelayCommand]
private void DeleteSaves() => DeleteConfirmRequest.Raise(
    "Delete all saves?",
    context => StatusText = $"Deleted: {context}");

// View — invoke args.Callback after the interaction ends
[Subscribe("DeleteConfirmRequest.Raised")]
private void OnDeleteConfirmRequested(object? sender, InteractionEventArgs<string> e)
{
    _dialog.ShowConfirm(e.Context, onConfirmed: () => e.Callback?.Invoke());
}
```

5. Thread discipline: Godot controls, bindings, and `ObservableCollection`s bound to `[ItemsSource]` can only be touched on the main thread. When a background thread produces data, marshal back with `IUiDispatcher` — capture it on the main thread (`UiDispatcher.CaptureCurrentOrImmediate()` in the `[ViewModelFactory]`, which runs inside `InitializeView`) and `Post` the update:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;

public partial class MatchViewModel : ViewModelBase
{
    private readonly IUiDispatcher _dispatcher;

    public MatchViewModel(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [ObservableProperty]
    private string _statusText = "Idle";

    private async Task FetchAsync()
    {
        var result = await _network.FetchAsync().ConfigureAwait(false);
        _dispatcher.Post(() =>
        {
            if (IsDisposed)
                return;
            StatusText = result;
        });
    }
}
```

The View supplies the dispatcher through the factory — `IUiDispatcher` is never registered in DI, so a bare interface constructor parameter would make the generator emit `GetRequiredService<IUiDispatcher>()` and throw at runtime:

```csharp
using DotPudica.Core.Binding;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;

[DotPudicaView(typeof(MatchViewModel))]
public partial class MatchView : Control
{
    public override void _Ready() => InitializeView();
    public override void _ExitTree() => DisposeView();

    [ViewModelFactory]
    private MatchViewModel CreateMatch()
        => new(UiDispatcher.CaptureCurrentOrImmediate());
}
```

High-frequency background updates (network telemetry, server snapshots) use `LatestSnapshotMailbox<T>` — the background thread publishes immutable snapshots (latest wins, intermediates dropped), the main thread drains once on demand:

```csharp
private readonly LatestSnapshotMailbox<RoomSnapshot> _mailbox = new();

// background producer thread — never touches the VM or the UI
_mailbox.Publish(new RoomSnapshot(playerCount));

// main thread (view _Process / timer) — drain and apply to the VM
if (_mailbox.TryDrainLatest(out var latest))
    PlayerCount = latest.PlayerCount;
```

Checkpoints: bindings refuse off-thread lifecycle operations (set the dispatcher before bindings are created); collections bound to `[ItemsSource]` must not be mutated item-by-item from a background thread — post the change or drain a snapshot instead.

6. Cancel scene-lifetime work when leaving the scene. Take the token from the scope, and give each long operation its own linked child token so cancelling one operation does not cancel the whole scene:

```csharp
var host = GetParent() as SceneContextHost;
var cts = host.Operations.CreateLinkedTokenSource();  // one operation only

try
{
    var result = await _network.FetchAsync(cts.Token).ConfigureAwait(false);
    _dispatcher.Post(() => StatusText = result);
}
catch (OperationCanceledException)
{
    // scene left — no UI update
}
```

Checkpoints: the host disposes `Operations` on `_ExitTree` (cancelling the token); after `Dispose`, `Token` access throws `ObjectDisposedException` while `Cancel` is a silent no-op.

7. Build with `dotnet build` and fix until zero DOTPUDICA diagnostics (look each ID up in `references/diagnostics.md`).

## Output Contract

Deliverable = the communication code block (ViewModel event + `[Subscribe]` handler, `MessageBus` send/receive pair, or `InteractionRequest` declaration + `Raise` + View subscription) +, in threading scenarios, the `Post` or `LatestSnapshotMailbox` code block +, when leaving a scene, the `SceneOperationScope` token wiring. Acceptance:

- `dotnet build` passes with zero DOTPUDICA diagnostics.
- Runtime checkpoints pass: when the VM event fires the View responds exactly once (switching scenes and returning must not produce repeated subscriptions — `DisposeView` detaches); background data updates reach the UI with no cross-thread errors; after leaving the scene, in-flight async callbacks no longer update the UI.

## Failure Handling

| Symptom | Fix action |
|---|---|
| `DOTPUDICA042` (`SubscribeInvalid`) | The `[Subscribe]` event path does not resolve on the ViewModel, or the handler signature is incompatible — fix the path, or align the handler (void return, parameter count and contravariant parameter types matching the event delegate). |
| Cross-thread exception when updating the UI from a callback | The update runs on a worker thread — wrap it in `IUiDispatcher.Post` instead of touching VM/UI directly. |
| Message never received | Weak bus: the recipient was GC'd before the message arrived — keep a reference to the recipient while it must receive; strong bus: you forgot the manual `MessageBus.UnregisterAllStrong(this)`. |
| `InteractionRequest` produces no response | `Raise` with no subscribers is a silent no-op — check the View actually subscribes (`[Subscribe("XxxRequest.Raised")]` or a manual `+=` on `Raised`). |
| Async callback updates a disposed VM after leaving the scene | Cancel via the scene `Token`/linked child token and guard the posted action with `IsDisposed`. |

The table only maps triggers; the full trigger condition, verbatim message, and fix action for the diagnostics live in `references/diagnostics.md` — look the ID up there.

## References

- `references/api-tour.md` — section 7 (MessageBus: weak/strong busses, `UnregisterAllStrong`), section 8 (InteractionRequest: `Raised`/`Raise`/callback semantics, no-subscriber no-op), section 9 (IUiDispatcher/UiDispatcher), section 10 (LatestSnapshotMailbox), section 11 (SceneOperationScope: `Token`/`CreateLinkedTokenSource`/dispose semantics).
- `references/diagnostics.md` — `DOTPUDICA042` entry.
- Related skills: `dotpudica-view` (`[Subscribe]` in views, lifecycle hooks), `dotpudica-di-scope` (`SceneContextHost.Operations` — the `SceneOperationScope` source), `dotpudica-route` (choosing which skill a task needs).
