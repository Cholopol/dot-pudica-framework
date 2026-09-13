## Highlights

- Fixed popups/dialogs briefly flashing at the scene origin `(0,0)` before fading in: `Create` (and pooled recycle) now set `Visible = false`, and `OnShow` runs on enter-animation **start**, before the window becomes visible — layout/centering done in `OnShow` is never seen by the player.
- Binding source generator migrated to the semantic `ForAttributeWithMetadataName` API: fully qualified attribute names (e.g. `[DotPudica.Godot.Views.DotPudicaView]`) now generate bindings, and views that inherit `[DotPudicaView]` from a base class get their own member bindings generated as overrides.

## Changes

- **Window Lifecycle**: `GodotWindow` sets `Visible = false` in its constructor, after `OnCreate`, and on pooled reset, so `AddChild` of a hidden window never paints it at the default origin.
- **Window Lifecycle**: `OnShow()` is now invoked from the enter transition `OnStart` (before `Visible = true`) instead of `OnEnd` (after the fade). Do Popup/Dialog centering and z-order layout in `OnShow`; to run logic after the fade completes, subscribe to `StateChanged` (`EnterAnimationEnd` / `Activated`).
- **Source Generator**: Attribute discovery now uses `ForAttributeWithMetadataName` instead of syntax-text matching, correctly resolving fully qualified / aliased / `global::`-prefixed `[DotPudicaView]`, `[BindTo]`, `[BindCommand]`, and `[ItemsSource]` usages.
- **Source Generator**: A partial class without its own `[DotPudicaView]` that inherits the attributed ViewModel from a base view (e.g. `ChildView : BaseView`) now emits `protected override void __DotPudicaInitializeBindingsCore()` that calls `base.__DotPudicaInitializeBindingsCore()` first, with no duplicated runtime field.
- **Source Generator**: Output is registered per view for finer incremental caching; member-bound views appearing via multiple attributes (`[BindTo]` / `[BindCommand]` / `[ItemsSource]`) are de-duplicated, and classes carrying their own `[DotPudicaView]` are never emitted twice.
- **Docs**: Window skill and API tour document the new `OnShow` timing and the `Visible = false` guarantee, including a troubleshooting row for "popup fades in at top-left then jumps to center".
- **Tests**: Added coverage for fully qualified `[DotPudicaView]` and inherited-attribute derived views; generator test harness now supports multiple generated sources.

## Breaking / migration

- No API or binary break. Note the **behavioral change** in `OnShow` timing: it now runs while the window is still hidden. If your override previously assumed the enter animation had finished, move that logic to a `StateChanged` subscription on `EnterAnimationEnd` / `Activated`.
