# DotPudica Diagnostics Reference

Source of truth: addons/dot-pudica source code (same repo/version). Verify against source when in doubt.

Quick lookup table for all 16 DOTPUDICA compile-time diagnostics reported by the source generator: ID, name, trigger condition, and fix action. Message quotes are verbatim from `SourceGenerator/DiagnosticDescriptors.cs` (placeholders `{0}`…`{3}` preserved, as substituted at the `ReportDiagnostic` call sites in `SourceGenerator/BindingGenerator.cs`). Whenever a build fails with a DOTPUDICA error, look the ID up here first.

## 1. Diagnostic table

All 16 diagnostics are `Error` severity. Categories: `DotPudicaBinding` (001, 005, 010, 030–036, 045), `DotPudicaLifecycle` (040–043, 046).

| ID | Name | Trigger condition | Fix action |
|---|---|---|---|
| `DOTPUDICA001` | PathNotFound | A binding path fails to resolve on the ViewModel type: any segment of a `[BindTo]` path, a `[BindCommand]` command name, an `[ItemsSource]` path, or an `[ItemsSource]` `ItemCommand` path names a member that does not exist (the message always reports the first segment). `Cannot resolve property path '{1}' on type '{0}': member '{2}' does not exist` | Fix the path or member name. For CommunityToolkit.Mvvm members the path must use the generated name — `[ObservableProperty]` fields resolve as their generated properties and `[RelayCommand]` methods as their generated commands (naming rules replicated in the generator; see attributes.md section 4). |
| `DOTPUDICA005` | CommandNotICommand | A `[BindCommand]` member resolves to a member whose type does not implement `System.Windows.Input.ICommand` (a final-member method, i.e. `[RelayCommand]`, is exempt). `Member '{1}' resolved from path '{0}' has type '{2}', which does not implement System.Windows.Input.ICommand` | Make the command property implement `ICommand` (via `[RelayCommand]` or a manual `ICommand` property). |
| `DOTPUDICA010` | CollectionNotObservable | An `[ItemsSource]` collection does not implement `System.Collections.Specialized.INotifyCollectionChanged`. `Member '{1}' resolved from path '{0}' has type '{2}', which does not implement System.Collections.Specialized.INotifyCollectionChanged and cannot be used for ItemsSource binding` | Use `ObservableCollection<T>` or another `INotifyCollectionChanged` collection. |
| `DOTPUDICA030` | StructIntermediatePath | An intermediate segment of a binding path is a value type (nullable value types are exempt), so chained `INotifyPropertyChanged` listening cannot be established. `Intermediate segment '{1}' of path '{0}' has value type '{2}', which cannot establish INotifyPropertyChanged chained listening` | Change the intermediate segment to a reference type. |
| `DOTPUDICA031` | TargetPropertyInvalid | The target property is empty (e.g. `Button` has no default bindable property) or not found on the control type. `No usable target property '{1}' found on control type '{0}'` | Set an explicit `[BindTo(Target=...)]`, or switch to a control with a bindable default property. |
| `DOTPUDICA032` | TypeMismatchWithoutConverter | Source and target types are incompatible (not the same type and not implicitly numeric-convertible) and no converter is provided. `Source type '{1}' of binding path '{0}' is incompatible with target type '{2}'; provide a Converter implementing IValueConverter<{1},{2}>, or use implicitly numeric convertible types` | Provide a `Converter` implementing the typed `IValueConverter<TSource,TTarget>`, or make the types equal / numerically convertible. |
| `DOTPUDICA033` | ConverterNotTyped | A `Converter` is provided but does not implement the typed `IValueConverter<TSource,TTarget>` for this binding's source/target pair. `Converter '{0}' does not implement IValueConverter<{1},{2}> and cannot be used for zero-allocation binding` | Implement the correct typed interface. |
| `DOTPUDICA034` | TwoWayReferenceUpcastRequiresConverter | The binding is a reference upcast (source is a base type of target) and the mode is `TwoWay` or `OneWayToSource`, which cannot safely write back to the source type. `Binding path '{0}' is a reference upcast ('{1}' → '{2}'); two-way/one-way-to-source binding cannot safely write back to the source type; provide an IValueConverter<{1},{2}>, or change to OneWay` | Provide a converter, or change the mode to `OneWay`. |
| `DOTPUDICA035` | BoxingConversionNotAllowed | The binding would box a value type (value type → `object` / `ValueType` / enum / interface), breaking the zero-allocation hot path. `Binding path '{0}' from source type '{1}' to target type '{2}' would cause boxing, breaking the zero-allocation hot path; use same-type binding, or provide an explicit IValueConverter<{1},{2}>` | Use a same-type binding, or provide an explicit converter. |
| `DOTPUDICA036` | ItemCommandParameterMismatch | An `[ItemsSource]` `ItemCommand` resolves to a `[RelayCommand]` method whose parameter type does not match the collection element type; the item template invokes the command with the element as the parameter. `ItemCommand '{0}' of [ItemsSource] has parameter type '{1}', which does not match element type '{3}' of collection '{2}'; the item template will invoke this command with the element as the parameter, please fix the command method parameter type` | Align the command method's parameter type with the collection element type. |
| `DOTPUDICA040` | ViewModelNotDiResolvable | `AutoInitialize=true` and no `[ViewModelFactory]`: the ViewModel is abstract, or has no exactly-one-public-constructor shape whose parameters are all interface types. `ViewModel '{0}' cannot be constructed by the generated factory: it must have exactly one public constructor whose parameters are all interface types, or the view must declare a [ViewModelFactory] method` | Satisfy the DI shape (concrete type, exactly one public constructor, all parameters interface types), or add a `[ViewModelFactory]` method. |
| `DOTPUDICA041` | ViewModelFactoryInvalid | A `[ViewModelFactory]` method violates the contract: must be a parameterless, non-static, non-void instance method returning the declared ViewModel type or a derived type. `The [ViewModelFactory] method '{0}' must be a parameterless instance method returning '{1}' or a derived type` | Fix the method to be a parameterless instance method returning the ViewModel type. |
| `DOTPUDICA042` | SubscribeInvalid | A `[Subscribe]` event path cannot be resolved on the ViewModel, or the handler signature is incompatible. `[Subscribe] event '{0}' could not be resolved on ViewModel '{1}', or handler '{2}' has an incompatible signature` | Fix the event path, or align the handler signature with the event delegate (void return, parameters matching the event's parameter list). |
| `DOTPUDICA043` | InjectNotWritable | An `[Inject]` member is a read-only field or a property without a setter. `The [Inject] member '{0}' must be a writable field or property` | Make the member writable. |
| `DOTPUDICA045` | VirtualizedItemsPoolSize | An `[ItemsSource]` on a `VirtualizedItemsControl` target sets `PoolSize > 0`. `[ItemsSource] on virtualized target '{0}' does not support PoolSize; virtualized controls manage their own recycling` | Remove `PoolSize`; virtualized controls manage their own recycling. |
| `DOTPUDICA046` | LifecycleEntryPointMissing | A required Godot lifecycle override is missing, or the override does not call the generated entry point. `View '{0}' must override '{1}()' and call '{2}()' — Godot only dispatches virtual overrides declared in user source, so the generated lifecycle must be wired from user-written Godot hooks` | Declare the missing `_Ready` / `_ExitTree` override(s) and call the required entry point per the matrix in section 2. |

## 2. DOTPUDICA046 lifecycle matrix

Godot only dispatches virtual overrides declared in user source — source generators cannot see each other's output, so the generator never re-generates `_Ready`/`_ExitTree`. The user must declare the overrides and wire the generated entry points. Required combination per `[DotPudicaView]` options:

| `Pooled` | `AutoInitialize` | `_Ready` must call | `_ExitTree` must call |
|---|---|---|---|
| `true` | any | `InitializeView()` | `RecycleView()` |
| `false` | `true` | `InitializeView()` | `DisposeView()` |
| `false` | `false` | — | `DisposeView()` |

Each row's check requires both halves: the override must be declared AND the call must be present (e.g. `Pooled=true` reports `DOTPUDICA046` if `_Ready` exists but does not call `InitializeView()`). The 040/041 factory checks only run when `AutoInitialize=true` (in both the pooled and non-pooled rows).

## 3. Diagnostic scope

The generator reports diagnostics only for the view that itself declares `[DotPudicaView]` (the view owns the DotPudica runtime). A derived view that inherits the attribute from a base class still gets bindings generated, but no DOTPUDICA diagnostics are reported for it — its binding errors surface as plain C# compile errors without a DOTPUDICA number.

## 4. Fix workflow

Any DOTPUDICA error → look up the ID in section 1 to locate the root cause → fix the source → rebuild (`dotnet build`) until zero DOTPUDICA diagnostics. The verbatim messages in section 1 are what the compiler prints (with placeholders substituted); use them to pinpoint the offending member.
