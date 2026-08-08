---
name: dotpudica-view
description: Create DotPudica View + ViewModel pairs with declarative bindings: [DotPudicaView], [BindTo], [BindCommand], [ItemsSource], [Inject], [Subscribe], [ViewModelFactory], lifecycle hooks and the mandatory _Ready/_ExitTree lines. Use whenever the user wants a UI page, panel, menu, list, a binding that does not work, or a property that does not update the UI.
---

# dotpudica-view

## Purpose

Build a page, panel, menu, or list as a `[DotPudicaView]` partial class bound to a ViewModel through declarative attributes, so UI state and commands flow automatically with zero hand-written plumbing. This is the I/O contract for page development: it defines what a valid View + ViewModel pair looks like, the exact steps to produce one, and the failure modes the source generator reports. It is the core dependency of the add-page workflow and of every fix where "a binding does not work" or "a property does not update the UI".

## Input Contract

Preconditions:

- Bootstrap is done: `AppContext.Initialize` has completed (otherwise run `dotpudica-bootstrap` first). Views resolve `[Inject]` services from `AppContext.Current.Services`.
- Godot 4.7.x .NET project with the DotPudica csproj block injected (see `dotpudica-bootstrap`).

Valid input states:

- ViewModel = `partial class` deriving from `ViewModelBase` or `ValidatableViewModelBase` (`DotPudica.Core.ViewModels`), or any `ObservableObject` (e.g. `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`).
- The ViewModel has exactly one public constructor whose parameters are all interface types (then no `[ViewModelFactory]` needed); any other construction shape requires a `[ViewModelFactory]` method — otherwise the generator reports `DOTPUDICA040` / `DOTPUDICA041`.
- View = `partial class` + `[DotPudicaView(typeof(TVM))]`, deriving from a Godot control.
- Control fields are `[Export]` (so the Godot editor stores the node reference) and already assigned in the scene inspector.
- Binding paths resolve statically: every intermediate path segment must be a reference type so chained `INotifyPropertyChanged` listening can be generated — a value-type intermediate segment reports `DOTPUDICA030`.

## Procedure

1. Design the ViewModel state, commands, and events. See the complete example in the next code block. Checkpoints: the class is `partial`; state fields use `[ObservableProperty]`, commands use `[RelayCommand]`; binding paths will use the *generated* names (`Title`, `SaveCommand`), not the field names (`_title`, `Save`) — see `references/attributes.md` section 4.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Settings";

    [ObservableProperty]
    private int _volume = 50;

    [RelayCommand]
    private void Save() { }
}
```

2. Write the View class with `[DotPudicaView(typeof(TVM))]`, `[Export]` control fields carrying the binding attributes, and the two lifecycle lines. Complete example:

```csharp
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

[DotPudicaView(typeof(SettingsViewModel))]
public partial class SettingsView : Control
{
    [Export, BindTo(nameof(SettingsViewModel.Title), Mode = BindingMode.OneWay)]
    private Label _title = null!;

    [Export, BindCommand(nameof(SettingsViewModel.SaveCommand))]
    private Button _saveButton = null!;

    public override void _Ready() => InitializeView();
    public override void _ExitTree() => DisposeView();

    partial void OnViewModelBound() { }
}
```

Checkpoint: `_Ready` and `_ExitTree` must both be declared AND call their entry points — Godot only dispatches overrides declared in user source, so missing either half reports `DOTPUDICA046` (use `RecycleView()` instead of `DisposeView()` when `Pooled = true`; see `references/lifecycle.md` section 2 for the matrix).
3. In the scene inspector, assign the controls to the `[Export]` fields declared in step 2 (this skill only guides; it never generates `.tscn` files). Checkpoint: every `[Export]` binding field has a node assigned — an unassigned field dereferences at runtime when the binding executes.
4. Add `[Subscribe]` handlers and lifecycle hooks (`OnViewReady`, `OnViewModelBound`, `OnViewDisposing`) where the feature needs them. Checkpoint: the event path must resolve on the ViewModel and the handler signature must match the event delegate (void return, matching parameters) — otherwise `DOTPUDICA042`.
5. For lists, bind with `[ItemsSource(path, itemScene)]`; set `ItemCommand` to a `[RelayCommand]` whose parameter type equals the collection element type. Checkpoints: the collection implements `INotifyCollectionChanged` (use `ObservableCollection<T>`), else `DOTPUDICA010`; the `ItemCommand` parameter matches the element type, else `DOTPUDICA036`; `PoolSize` is only for non-virtualized targets — on `VirtualizedItemsControl` it errors with `DOTPUDICA045`.
6. Build with `dotnet build` and fix until zero DOTPUDICA diagnostics (look each ID up in `references/diagnostics.md`). Checkpoint: zero diagnostics; when the project builds with `-p:EmitCompilerGeneratedFiles=true`, `obj/Generated` also contains `{Namespace}.{Class}.Bindings.g.cs` with `InitializeView` / `DisposeView` and the generated binding statements.

## Output Contract

Deliverable = the ViewModel file + the View file + a scene where every `[Export]` binding field is assigned. Acceptance:

- `dotnet build` passes with zero DOTPUDICA diagnostics.
- Runtime checkpoints pass: initial values display on open; command binding fires (button `pressed` → `SaveCommand`); subscriptions do not leak — switching scenes and returning must not produce repeated-subscription errors (bindings are released by `DisposeView`).
- Mutating an `[ObservableProperty]` updates the bound control without manual refresh.

## Failure Handling

| Symptom (typical trigger) | See `references/diagnostics.md` entry |
|---|---|
| Binding path typo, or Mvvm member written as field name (`_title`) instead of generated name (`Title`) | `DOTPUDICA001` |
| `[BindCommand]` points at a member whose type is not `ICommand` | `DOTPUDICA005` |
| `[ItemsSource]` collection is a plain `List<T>` / array | `DOTPUDICA010` |
| Path has a value-type intermediate segment (e.g. a struct inside a chained path) | `DOTPUDICA030` |
| Control has no usable default target property (e.g. `Button`, custom control not in the inference table) | `DOTPUDICA031` |
| Source and target types incompatible and no `Converter` given | `DOTPUDICA032` |
| `Converter` provided but does not implement the typed `IValueConverter<TSource,TTarget>` | `DOTPUDICA033` |
| Reference upcast (base → derived target) with `TwoWay` / `OneWayToSource` | `DOTPUDICA034` |
| Binding would box a value type (value type → `object` / interface / enum) | `DOTPUDICA035` |
| `ItemCommand` parameter type does not match the collection element type | `DOTPUDICA036` |
| `AutoInitialize = true`, no `[ViewModelFactory]`, and the VM is abstract or its constructor is not all-interface | `DOTPUDICA040` |
| `[ViewModelFactory]` method violates the shape (not parameterless instance method returning the VM type) | `DOTPUDICA041` |
| `[Subscribe]` event path unresolved or handler signature incompatible | `DOTPUDICA042` |
| `[Inject]` member is read-only (readonly field / property without setter) | `DOTPUDICA043` |
| `_Ready` / `_ExitTree` missing, or present but not calling the entry point | `DOTPUDICA046` |

The table only maps triggers; the full trigger condition, verbatim message, and fix action live in `references/diagnostics.md` — look the ID up there.

## References

- `references/attributes.md` — attribute parameters, defaults, `BindingMode` semantics, control inference table, cross-generator naming rules.
- `references/lifecycle.md` — `InitializeView` / `DisposeView` / `RecycleView` / `ActivateViewModel` step order, `AutoInitialize` x `Pooled` matrix, hook timing.
- `references/diagnostics.md` — full diagnostic table and the `DOTPUDICA046` lifecycle matrix.
- Related skills: `dotpudica-verify` (verifying a built page end-to-end), `dotpudica-di-scope` (service registration and `[Inject]` resolution), `dotpudica-route` (choosing which skill a task needs); prerequisite: `dotpudica-bootstrap`.
