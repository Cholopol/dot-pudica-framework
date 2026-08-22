## Highlights

- Window pooling from scenes: `IWindowManager` / `GodotWindowManager` now support configuring window pools using a scene path or a preloaded `PackedScene`
- Generic constraint relaxed on `ShowPooled<TWindow>` from `where TWindow : GodotWindow, new()` to `where TWindow : GodotWindow`, allowing scene-instantiated complex windows to be pooled and reused seamlessly

## Changes

- **Window Management**: Added `ConfigurePool<TWindow>(string scenePath, int maxSize)` and `ConfigurePool<TWindow>(PackedScene scene, int maxSize)` overloads to `IWindowManager` and `GodotWindowManager`.
- **API Relaxation**: Removed `new()` constraint on `ShowPooled<TWindow>`, aligning window pooling capabilities with `NodePool`.
- **Lifecycle Consistency**: Scene-instantiated pooled windows strictly adhere to `_Ready` -> `InitializeView()` and `_ExitTree` -> `RecycleView()` lifecycle contracts.

## Breaking / migration

- None. 100% backward compatible.
