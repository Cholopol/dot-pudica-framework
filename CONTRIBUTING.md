# Contributing Guide

[English](#contributing-guide) | [简体中文](#贡献指南)

## Branches

| Branch | Purpose |
| --- | --- |
| `master` | Stable release line; default checkout for consumers |
| `v1.x` | 1.x integration line for version management and PR merges |

## How to contribute

1. Fork this repo and branch from the latest `v1.x` (do not commit directly to `master`).
2. One concern per PR; run local verification, then open a PR **targeting `v1.x`**.
3. Maintainers merge into `v1.x`; releases sync / merge to `master` and add a tag.
4. Game projects should install only `addons/dot-pudica`. Keep `tests/`, `benchmarks/`, and `samples/` in this repo—do not ship them into consumer projects.

## Environment

- Godot **4.7.x** .NET
- .NET 8 SDK

## Hard rules

1. **`DotPudica.Core` must not reference Godot**; engine types belong in `DotPudica.Godot`.
2. **Touch Godot objects only on the main thread**; background work may produce immutable results or post to the UI dispatcher.
3. Every View must declare `_Ready() => InitializeView();` / `_ExitTree() => DisposeView();` (missing either → `DOTPUDICA046`). Do not emit these overrides from generated code.
4. No expression trees / reflection on binding hot paths; mutate bound collections only on the UI thread.
5. Reuse existing binding and dispatch abstractions; no parallel pipelines or patch-style bool flags.

## Verification

```powershell
dotnet build DotPudicaFramework.sln
dotnet test tests/DotPudica.Tests/DotPudica.Tests.csproj
# GODOT_BIN must point to the Godot 4.7.x .NET console; build before headless runs
& $env:GODOT_BIN --headless --path . res://tests/DotPudica.Integration/IntegrationTestRunner.tscn
```

## Versioning and releases

| | Meaning | Where |
| --- | --- | --- |
| Framework version | Plugin package / Release | Root `VERSION`, `plugin.cfg` `version`, git tag `v*` |
| Godot baseline | Engine compatibility (currently 4.7.1) | `Godot.NET.Sdk/4.7.1`; `plugin.cfg` **description** |

On `v1.x` (or an aligned `master`), set the framework version in `VERSION` and `plugin.cfg`, then:

```powershell
git add VERSION addons/dot-pudica/plugin.cfg
git commit -m "chore: release 1.1.0"
git push
git tag v1.1.0
git push origin v1.1.0   # Triggers Actions: zip + GitHub Release
```

In release notes, `#12` links to this repo’s issues/PRs. You can also commit/push with GitHub Desktop, then create the tag with the two commands above.

## License

MIT.

---

# 贡献指南

[English](#contributing-guide) | [简体中文](#贡献指南)

## 分支

| 分支 | 用途 |
| --- | --- |
| `master` | 稳定发布线；对外默认检出点 |
| `v1.x` | 1.x 版本管理与 PR 合并线；日常贡献提向此分支 |

## 怎么参与

1. Fork 本仓库，从最新 `v1.x` 拉功能分支（勿直接改 `master`）。
2. 一次 PR 只做一件事；改完本地验证后，**PR 目标选 `v1.x`**。
3. 维护者审阅合并进 `v1.x`；发版时再合入 / 对齐 `master` 并打 tag。
4. 游戏工程只需安装 `addons/dot-pudica`；`tests/`、`benchmarks/`、`samples/` 留在本仓，不要打进消费工程。

## 环境

- Godot **4.7.x** .NET
- .NET 8 SDK

## 必须遵守

1. **`DotPudica.Core` 不得引用 Godot**；引擎类型只放在 `DotPudica.Godot`。
2. **Godot 对象只在主线程访问**；后台只产出不可变结果或向 UI 调度投递。
3. View 必须手写 `_Ready() => InitializeView();` / `_ExitTree() => DisposeView();`（漏写 `DOTPUDICA046`）。不要在生成代码里伪造这两行覆盖。
4. 绑定热路径禁止表达式树 / 反射读写路径；集合只在 UI 线程改。
5. 先复用现有绑定与调度抽象，禁止平行管线；不堆补丁式 bool 状态。

## 验证

```powershell
dotnet build DotPudicaFramework.sln
dotnet test tests/DotPudica.Tests/DotPudica.Tests.csproj
# 需 GODOT_BIN 指向 Godot 4.7.x .NET 控制台；跑集成前先 build
& $env:GODOT_BIN --headless --path . res://tests/DotPudica.Integration/IntegrationTestRunner.tscn
```

## 版本与发版

| | 含义 | 位置 |
| --- | --- | --- |
| 框架版本 | 插件包 / Release | 根目录 `VERSION`、`plugin.cfg` 的 `version`、tag `v*` |
| Godot 基线 | 引擎兼容（当前 4.7.1） | `Godot.NET.Sdk/4.7.1`；`plugin.cfg` 的 **description** |

在 `v1.x`（或已对齐的 `master`）上改好 `VERSION` 与 `plugin.cfg` 的框架版本后：

```powershell
git add VERSION addons/dot-pudica/plugin.cfg
git commit -m "chore: release 1.1.0"
git push
git tag v1.1.0
git push origin v1.1.0   # 触发 Actions：上传 zip 并创建 Release
```

Release 说明里写 `#12` 会链接到本仓 Issue/PR。也可用 GitHub Desktop 提交推送，再用上面两行打 tag。

## 许可证

MIT。
