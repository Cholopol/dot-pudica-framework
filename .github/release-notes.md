## Highlights

- First public addon package for Godot **4.7.x** .NET / .NET 8 (`addons/dot-pudica` only)
- Compile-time MVVM: `[BindTo]` / `[BindCommand]` / `[ItemsSource]`, AOT-friendly source generation
- Collection binding for `ObservableCollection` / `INotifyCollectionChanged`, including virtualized lists (#1)
- Dual-machine desktop benchmarks (macOS / Windows) plus iOS NativeAOT notes; AI assistant skills under `Skills/`

## Changes

- Added `[ItemsSource]` list binding driven by `INotifyCollectionChanged` (use `ObservableCollection<T>`): item add/remove refreshes the View automatically — addresses #1 (WPF-style ItemsControl / collection datasource)
- Added `VirtualizedItemsControl` for large lists (stable active node count under heavy row counts)
- Added declarative views, commands, converters, DI (`AppContext` / scene Scope), windows, pooling, messaging & UI-thread coalescing
- Added headless integration coverage, Core + Godot benchmarks, and English docs (`README`, benchmark reports)
- Packaged Release zip so game projects install only `addons/dot-pudica/` (do not copy `tests/` / `benchmarks/` / `samples/`)

## Breaking / migration

- None (first public package). Baseline: Godot **4.7.x** .NET, .NET 8.
- Collections bound with `[ItemsSource]` may only be mutated on the Godot main thread.

## Thanks

- @sericaer for #1

