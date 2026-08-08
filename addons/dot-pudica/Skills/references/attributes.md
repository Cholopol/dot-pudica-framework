# DotPudica Attribute Reference

Source of truth: addons/dot-pudica source code (same repo/version). Verify against source when in doubt.

Quick lookup table for the seven framework attributes: parameters, defaults, valid targets, compile-time inference rules, and the cross-generator naming rules. Read this reference before writing any view, binding, or composition code; every fact here mirrors the attribute definitions and the source generator's inference logic.

## 1. Attribute reference

| Attribute | Namespace | Valid targets | Constructor / parameters | Defaults & rules |
|---|---|---|---|---|
| `[DotPudicaView(Type viewModelType)]` | `DotPudica.Godot.Views` | class | `viewModelType` (required): the ViewModel type | `Ownership = ViewModelOwnership.Owned` (create + dispose on teardown); `AutoInitialize = true` (full generated lifecycle: injection, VM creation, SetViewModel, DotPudicaInitialize, subscriptions, dispose); `Pooled = false` (true emits `RecycleView()` / `ActivateViewModel(vm)` for node pooling). `ViewModelOwnership` lives in `DotPudica.Core.ViewModels`. |
| `[BindTo(string path)]` | `DotPudica.Core.Binding.Attributes` | field / property | `path` (required): ViewModel property path, supports nesting, e.g. `"Account.Username"` | `Mode = BindingMode.Default` (compile-time inference, see section 2); `Converter` (`Type?`, must implement the typed `IValueConverter<TIn,TOut>`); `Target` (`string?`, overrides the target control property name when inference fails); `Signal` (`string?`, overrides the control change signal name when inference fails). |
| `[BindCommand(string commandName)]` | `DotPudica.Core.Binding.Attributes` | field / property | `commandName` (required): ICommand property name on the ViewModel | `Parameter` (`string?`, ViewModel property path passed as the command parameter); `Signal` (`string?`, default inferred from the control type — only `Button` / `LinkButton` / `BaseButton` resolve to `pressed`, matched by exact type name without inheritance walk; other controls need an explicit `Signal`). |
| `[ItemsSource(string path, string itemScene)]` | `DotPudica.Core.Binding.Attributes` | field / property | `path` (required): collection property path on the ViewModel, supports nesting, e.g. `"Inventory.Items"`; `itemScene` (required): item template `PackedScene` resource path | `PoolSize = 0` (recycled item-view capacity; `0` = destroy removed views immediately; pooling is opt-in); `ItemCommand` (`string?`, ICommand property path on the ViewModel — typically from `[RelayCommand]` — injected into each item view's `IItemsControlItemCommand.ItemCommand` with the parameter fixed to the row's DataContext; when it points at a `[RelayCommand]` method, the generator validates the method parameter type against the collection element type, else `DOTPUDICA036`). Collection must implement `INotifyCollectionChanged`, and can only be modified on the UI thread. Use `VirtualizedItemsControl` (a field whose type name is `VirtualizedItemsControl` or derived) instead for large lists; `PoolSize` on virtualized targets errors with `DOTPUDICA045`. |
| `[Inject]` | `DotPudica.Core.Composition` | field / property | none | The generator resolves the service from the application context and assigns it before any user hook runs. Member must be writable (`DOTPUDICA043` otherwise). |
| `[ViewModelFactory]` | `DotPudica.Core.Composition` | method | none | Custom ViewModel construction when the VM constructor is not DI-resolvable. Must be a parameterless, non-static, non-void instance method whose return type is the `[DotPudicaView]`-declared ViewModel type or a derived type; any violation reports `DOTPUDICA041`. |
| `[Subscribe(string eventPath)]` | `DotPudica.Core.Composition` | method (repeatable, one event per attribute) | `eventPath` (required): event property path on the ViewModel, supports chained paths, e.g. `"LoginSucceeded"` or `"Sub.Vm.Event"` | The generator subscribes after bindings initialize and unsubscribes during teardown. Unresolvable event or incompatible handler signature reports `DOTPUDICA042`. |

## 2. BindingMode semantics

Enum `DotPudica.Core.Binding.BindingMode`:

| Member | Meaning |
|---|---|
| `Default` | Resolved at compile time by the generator from the control type: input controls resolve to `TwoWay`, display-only controls to `OneWay`. Mechanically the generator checks whether the control's inferred change signal is non-null (`BindingGenerator.InferTargetAndSignal` + mode inference in `GenerateBindingCode`): a signal means `TwoWay`, no signal means `OneWay`. Outside the generator, `Default` behaves as `OneWay`. |
| `OneWay` | ViewModel → View. |
| `TwoWay` | ViewModel ↔ View. |
| `OneTime` | ViewModel → View once, at initial binding only. |
| `OneWayToSource` | View → ViewModel (reverse). |

## 3. Control default property / signal inference table

`Constants.ControlDefaults` in the source generator (exact table, `Property` + change `Signal`). For `[BindTo]` the generator walks the inheritance chain, so a custom control deriving from a built-in inherits its defaults (matched by type name, `StringComparer.Ordinal`):

| Control type | Default property | Change signal |
|---|---|---|
| `Label` | `Text` | (none) |
| `RichTextLabel` | `Text` | (none) — also supports `BbcodeText`; the built-in proxy takes a constructor flag and bbcode mode is selected by binding `Target=BbcodeText` |
| `LineEdit` | `Text` | `text_changed` |
| `TextEdit` | `Text` | `text_changed` |
| `SpinBox` | `Value` | `value_changed` |
| `HSlider` / `VSlider` / `Slider` | `Value` | `value_changed` |
| `CheckBox` / `CheckButton` | `ButtonPressed` | `toggled` |
| `OptionButton` | `Selected` | `item_selected` |
| `ProgressBar` | `Value` | (none) — read-only, no change signal |
| `TextureRect` | `Texture` | (none) — read-only |
| `Button` / `LinkButton` / `BaseButton` | (none — no property) | `pressed` — used for command binding |

Range family notes:

- Any `Godot.Range`-derived control (name suffix `ProgressBar`, `Slider`, `HSlider`, `VSlider`, `SpinBox`, or `Range`) binds its numeric targets through `DotPudica.Godot.Binding.GodotRangeBinding` for coordinated writes (e.g. `MinValue`/`MaxValue` writes are applied together).
- Bindable range target property names (exact, per `RangeBindingProperty` enum / `RangeBindingTargetNames`): `Value`, `MinValue`, `MaxValue`. There is no `Min`/`Max` target — always write the full names above.

Command signals (`Constants.CommandSignals`, exact type-name match, no inheritance walk): `Button`, `LinkButton`, `BaseButton` → `pressed`. A `[BindCommand]` on any other control type must set `Signal` explicitly.

Unknown control types (no entry in `ControlDefaults`, no inherited entry) get no inferred property/signal — supply `[BindTo(Target=..., Signal=...)]` overrides; an unresolvable target reports `DOTPUDICA031`.

## 4. Cross-generator naming rules

CommunityToolkit.Mvvm generated members are invisible to the DotPudica generator (generators do not see each other's output), so binding paths are resolved by replicating the Mvvm naming rules against the user-declared fields/methods:

| Source declaration | Generated property name | Rule (replicated in `BindingGenerator.GetGeneratedPropertyName` / `GetGeneratedCommandName`) |
|---|---|---|
| `[ObservableProperty] private string _title;` | `Title` | Strip prefix: `m_` / `s_` / `t_` (2 chars) or `_` (1 char), then uppercase the first character (`char.ToUpperInvariant`). BindTo/ItemsSource/Parameter paths must use the generated name, e.g. `[BindTo("Title")]`. |
| `[ObservableProperty] private string m_title;` / `s_title` / `t_title` | `Title` | Same strip-and-uppercase rule. |
| `[RelayCommand] void Save()` | `SaveCommand` | Append `Command`. |
| `[RelayCommand] Task SaveAsync()` | `SaveCommand` | Strip trailing `Async` first, then append `Command` — `SaveAsync` does NOT become `SaveAsyncCommand`. |
| `[RelayCommand] void OnItemSelected(T item)` | `OnItemSelectedCommand` | Append `Command`; the generated command type is treated as `ICommand` by the generator, and the parameter type is validated against the collection element type when used as `ItemCommand`. |

## 5. Common errors

| Symptom | Diagnostic | Fix |
|---|---|---|
| Binding path typo (`[BindTo]` path, `[BindCommand]` command name, `[ItemsSource]` path, `Parameter` path) | `DOTPUDICA001` PathNotFound — "Cannot resolve property path '{1}' on type '{0}': member '{2}' does not exist" | Fix the path; for Mvvm members, write the generated name per section 4. See `diagnostics.md` for the full diagnostic list. |
| Control type not in the inference table | `DOTPUDICA031` TargetPropertyInvalid — "No usable target property found on control type" | Override with `[BindTo(Target=..., Signal=...)]`. |
| `[BindCommand]` on a control without a default signal (non-Button family) | — | Set `Signal` explicitly, e.g. `[BindCommand("Save", Signal = "toggled")]`. |
| `[ItemsSource]` element type mismatch | `DOTPUDICA036` ItemCommandParameterMismatch | Align the command method parameter with the collection element type. |
