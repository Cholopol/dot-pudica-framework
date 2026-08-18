## Highlights

- Source generator type display rework: nullable-consistent `ForCode()` formatting plus built-in proxy target type alignment
- Strengthened generator regression suite (nullable / converter / delegate-proxy scenarios)
- Fixed benchmark setup chart generation (BDN `BindingSetup`)

## Changes

- Source generator: type display now emits nullable reference type modifiers (`ForCode()` in `TypeDisplay.cs`), so generated `BindProperty<TFrom, TTo>` signatures match runtime proxy semantics exactly
- Source generator: added `AlignTargetTypeWithBuiltInProxy` — built-in proxy target value types are resolved from the proxy interface, eliminating `string?` vs `string` mismatches for `LabelProxy` / `TextureRectProxy` / range controls
- Tests: added nullable proxy harness and regression tests (nullable converter target, non-nullable `string` target, delegate proxy without nullable warnings)
- Benchmarks: regenerated setup chart with BDN `BindingSetup` (`chart-binding-setup.png`, report script refactor)

## Breaking / migration

- None. Behavior-compatible refactor of generated binding type display.

