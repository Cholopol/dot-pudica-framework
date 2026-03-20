# DotPudica Framework - A Data-Driven Framework for Godot .NET

<div align="center">

<img src=".github/banner.png" alt="DotPudica Preview" width="50%"/>

</div>

![Godot](https://img.shields.io/badge/Godot-4.6+-478CBF?style=flat-square&logo=godotengine&logoColor=white) ![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white) ![C#](https://img.shields.io/badge/C%23-12-239120?style=flat-square&logo=csharp&logoColor=white) ![MVVM](https://img.shields.io/badge/Architecture-MVVM-0A7E8C?style=flat-square) ![Source Generator](https://img.shields.io/badge/Roslyn-Source_Generator-CB4B16?style=flat-square) ![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux%20%7C%20Android%20%7C%20Web-6C757D?style=flat-square) ![License](https://img.shields.io/badge/License-MIT-green?style=flat-square) ![Status](https://img.shields.io/badge/Status-Prototype-orange?style=flat-square) [![Stars](https://img.shields.io/github/stars/dot-pudica/dot-pudica-framework?style=flat-square&logo=github&color=yellow)](https://github.com/dot-pudica/dot-pudica-framework/stargazers) [![Forks](https://img.shields.io/github/forks/dot-pudica/dot-pudica-framework?style=flat-square&logo=github)](https://github.com/dot-pudica/dot-pudica-framework/network/members)

[English](README.md) | 简体中文

DotPudica 是一个面向 Godot 4.6 + .NET 8 的轻量级 MVVM 框架。把传统 .NET UI 的绑定模型迁移到 Godot 的节点式界面中，让 View 负责控件与表现，让 ViewModel 负责状态、命令、消息与业务流程。致力于实现无感便捷的游戏UI开发体验。

DotPudica 仓库包含：

- 一组可以直接在 Godot 中验证的示例场景；
- 一套可运行的 MVVM 框架基础设施：

| 项目                                  | 作用              | 关键词                                                |
| ----------------------------------- | --------------- | -------------------------------------------------- |
| `DotPudicaFramework`                | Godot 主工程与示例入口  | `project.godot`、示例场景、调试载体                          |
| `addons/dot-pudica/Core`            | 与引擎无关的 MVVM 核心层 | `BindingContext`、`PropertyBinding`、`ViewModelBase` |
| `addons/dot-pudica/Godot`           | Godot 适配层       | `DotPudicaViewRuntime`、控件代理、日志桥接                   |
| `addons/dot-pudica/SourceGenerator` | Roslyn 编译期生成层   | `[DotPudicaView]`、`[BindTo]`、`[BindCommand]`       |

运行链路可以概括为：

```text
Godot Control / Node
    ↓ 通过 [Export] 挂接控件
Partial View Stub
    ↓ 通过 [DotPudicaView] / [BindTo] / [BindCommand] 声明意图
Source Generator
    ↓ 生成 ViewModel、BindingContext、初始化与绑定代码
DotPudicaViewRuntime<TViewModel>
    ↓ 建立 BindingContext
PropertyBinding / CommandBinding
    ↓ 通过 ITargetProxy 与 Godot 控件交互
ViewModel / ICommand / Messenger / Services
```

这一分层的价值在于：

- 核心绑定逻辑不直接依赖具体场景结构
- Godot 控件差异被 `ITargetProxy` 层吸收
- View 样板代码被 Source Generator 吸收
- ViewModel 仍可使用 CommunityToolkit.Mvvm 的成熟能力

## 技术栈&#x20;

本项目当前建立在以下技术组合之上：

- `Godot.NET.Sdk/4.6.1`
- .NET 8 主运行时，Android 目标预留 `net9.0`
- `CommunityToolkit.Mvvm` 用于 `ObservableProperty`、`RelayCommand`、`Messenger`、`Ioc`
- `Microsoft.Extensions.DependencyInjection` 用于服务容器
- Roslyn Incremental Source Generator 用于自动生成 View 绑定样板代码

这种组合的优点很直接：

- ViewModel 写法接近标准 .NET MVVM，没有额外 DSL 负担
- 绑定声明贴近控件字段，可读性很高
- 生成器在编译期完成样板拼装，避免运行时结构扫描
- 运行时仍保留反射兜底能力，兼顾扩展性

## 快速开始 - 让UI像含羞草般绽放

### 1. 打开工程

- 使用 Godot 4.6.1 打开根目录下的 `project.godot`
- 确保本机已安装对应 .NET SDK

### 2. 编译项目

Godot .NET 项目由 Godot 编辑器管理 .NET 编译，无需手动敲 `dotnet build`。

**方式一：编辑器内编译（推荐）**

- 使用 Godot 4.6.1 打开项目后，直接按 `Ctrl + Shift + B` 或点锤子图标 🔨 **Build** 触发编译
- Godot 会自动识别 `DotPudica.Core`、`DotPudica.Godot`、`DotPudica.SourceGenerator` 三个 addon 项目的引用关系并按正确顺序编译

**方式二：命令行编译**

```bash
dotnet build DotPudicaFramework.sln
```

**方式三：导出时完整编译**

- 在 **Project → Export** 中选择目标平台（Windows/macOS/Linux/Android/Web）导出时，Godot 会执行一次完整编译，确保所有依赖和源码生成器输出均已纳入最终产物

### 3. 编写一个最小 View

```csharp
[DotPudicaView(typeof(MyPanelViewModel))]
public partial class MyPanelView : Control
{
    [Export, BindTo(nameof(MyPanelViewModel.Title), Mode = BindingMode.OneWay)]
    private Label _title = null!;

    public override void _Ready()
    {
        ViewModel = new MyPanelViewModel();
        DotPudicaInitialize();
    }

    public override void _ExitTree()
    {
        DotPudicaDispose();
        base._ExitTree();
    }
}
```

### 4. 编写对应 ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

public partial class MyPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Hello DotPudica";
}
```

## ViewModel 基类 - 内功与呼吸法

### `ViewModelBase` - 日志、消息与生命周期

`ViewModelBase` 继承 `ObservableObject`，并内置：

- 懒加载日志对象 `Log`
- 弱引用消息总线 `Messenger`
- `Send<TMessage>()` 与 `Register<TMessage>()` 快捷方法
- `Dispose()` 生命周期回收入口

推荐样板：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    private bool CanLogin()
        => !string.IsNullOrWhiteSpace(Username)
           && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = "";
        await Task.Delay(500);
        Send(new DotPudica.Core.Messaging.NotificationMessage("LoginSuccess"));
    }
}
```

### `ValidatableViewModelBase` - 数据注解验证与表单校验

`ValidatableViewModelBase` 继承 `ObservableValidator`，适合表单型界面：

- 支持 `[Required]`、`[Range]`、`[EmailAddress]` 等内置验证注解
- 提供 `ValidateAll()` 统一验证入口
- 适合注册、角色创建、设置页等需要约束输入的场景

推荐样板：

```csharp
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

public partial class RegistrationViewModel : ValidatableViewModelBase
{
    [ObservableProperty]
    [Required(ErrorMessage = "用户名不能为空")]
    [MinLength(3, ErrorMessage = "用户名至少 3 个字符")]
    private string _username = "";

    [RelayCommand]
    private void Submit()
    {
        if (!ValidateAll())
            return;
    }
}
```

## 数据绑定 - 从“触碰”到“害羞”之间

### 已实现的绑定方式

当前框架已经实现两条主链路：

| 绑定类型 | 入口特性            | 说明                      |
| ---- | --------------- | ----------------------- |
| 属性绑定 | `[BindTo]`      | 将控件属性与 ViewModel 属性路径连接 |
| 命令绑定 | `[BindCommand]` | 将控件事件绑定到 `ICommand`     |

### 绑定模式

`[BindTo]` 支持以下模式：

| 模式               | 方向                 | 典型控件                           |
| ---------------- | ------------------ | ------------------------------ |
| `OneWay`         | ViewModel -> View  | `Label`、`ProgressBar`          |
| `TwoWay`         | ViewModel <-> View | `LineEdit`、`CheckBox`、`Slider` |
| `OneWayToSource` | View -> ViewModel  | 输入主导型场景                        |
| `OneTime`        | 首次同步后不再更新          | 静态展示                           |
| `Default`        | 自动推断               | 有输入信号则推断为 `TwoWay`，否则 `OneWay` |

### 支持的默认控件推断

生成器通过 `Constants.ControlDefaults` 内置了常见控件推断规则，例如：

- `LineEdit -> Text / text_changed`
- `TextEdit -> Text / text_changed`
- `SpinBox -> Value / value_changed`
- `CheckBox -> ButtonPressed / toggled`
- `OptionButton -> Selected / item_selected`
- `ProgressBar -> Value`
- `Label -> Text`
- `Button -> pressed`

很多时候只写：

```csharp
[Export, BindTo("Username")]
private LineEdit _usernameInput = null!;
```

生成器就可以自动推断出目标属性与变化信号。

### 值转换器

当 ViewModel 值类型与控件表达方式不完全一致时，可以通过 `Converter` 接入转换器：

```csharp
[Export, BindTo("IsLoading", Mode = BindingMode.OneWay,
    Converter = typeof(BoolToVisibilityConverter))]
private ProgressBar _loadingBar = null!;
```

适用场景包括：

- 布尔值到可见性
- 枚举到文本
- 数值到样式或颜色
- 数据模型到 Godot 资源对象

### 命令绑定

命令绑定把 Godot 控件事件转接到 `ICommand`：

```csharp
[Export, BindCommand("LoginCommand")]
private Button _loginButton = null!;
```

ViewModel 中只需：

```csharp
[RelayCommand]
private void Login()
{
    // 业务逻辑
}
```

### &#x20;

## View 层写法 - 监督UI的调度板

一个典型 View 通常只做四件事：

1. 用 `[Export]` 挂接 Godot 控件
2. 用 `[BindTo]` / `[BindCommand]` 声明绑定
3. 在 `_Ready()` 中创建或注入 `ViewModel`
4. 在 `_ExitTree()` 中调用 `DotPudicaDispose()`

推荐样板：

```csharp
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

[DotPudicaView(typeof(PlayerHudViewModel))]
public partial class PlayerHudView : Control
{
    [Export, BindTo(nameof(PlayerHudViewModel.HealthText), Mode = BindingMode.OneWay)]
    private Label _healthLabel = null!;

    [Export, BindTo(nameof(PlayerHudViewModel.HealthPercent), Mode = BindingMode.OneWay)]
    private ProgressBar _healthBar = null!;

    [Export, BindCommand(nameof(PlayerHudViewModel.UsePotionCommand))]
    private Button _usePotionButton = null!;

    public override void _Ready()
    {
        ViewModel = new PlayerHudViewModel();
        DotPudicaInitialize();
    }

    public override void _ExitTree()
    {
        DotPudicaDispose();
        base._ExitTree();
    }
}
```

## ViewModel 层写法 - 引擎无关的UI调度者

ViewModel 的职责不是控制节点树，而是描述状态变化和业务意图：

- 把 Godot 场景结构留在 View
- 把可测试的业务状态留在 ViewModel
- 通过 `ObservableProperty` 与计算属性组合界面表现
- 通过 `RelayCommand` 组织交互动作
- 通过 `Send()` 或 `MessageBus` 推送跨模块消息

推荐样板：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

public partial class PlayerHudViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HealthPercent), nameof(HealthText))]
    private double _health = 80;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HealthPercent), nameof(HealthText))]
    private double _maxHealth = 100;

    public double HealthPercent => MaxHealth <= 0 ? 0 : Health / MaxHealth * 100;
    public string HealthText => $"HP: {(int)Health}/{(int)MaxHealth}";

    [RelayCommand]
    private void UsePotion()
    {
        Health = Math.Min(MaxHealth, Health + 25);
    }
}
```

## 源码生成 - 源生，启动！

`addons/dot-pudica/SourceGenerator/BindingGenerator.cs` 是 DotPudica 开发体验的关键。它不是一个“帮你少写几行代码”的辅助脚本，而是把 View 的声明式写法翻译成可运行绑定基础设施的编译期引擎。

### 设计目标 - 让样板消失在编译期

DotPudica 的 Source Generator 主要解决三个问题：

1. 避免开发者在每个 View 里重复编写 `BindingContext`、`ViewModel` 属性和初始化代码
2. 把“控件字段上的绑定声明”转译成实际运行时调用
3. 在保留 Godot 原生 View 写法的同时，注入 MVVM 所需的运行时骨架

换句话说，开发者编写的是“声明”，生成器补足的是“结构”和“连线”。

### 技术选型 - 为什么使用 Incremental Generator

DotPudica 选择的是 Roslyn `IIncrementalGenerator`，而不是旧式一次性 Generator。原因在于增量生成器更适合 UI 框架这种频繁编辑、频繁编译的开发场景。

它带来的好处包括：

- 只处理相关语法节点，避免全量扫描整个项目
- 支持语法过滤与语义分析分阶段执行
- 设计上更适合大型解决方案中的持续增量构建
- 对 IDE 内即时反馈更友好

在实现上，生成器入口是：

```csharp
[Generator]
internal sealed class BindingGenerator : IIncrementalGenerator
```

初始化阶段通过：

```csharp
context.SyntaxProvider.CreateSyntaxProvider(...)
```

构建出一条从语法节点到绑定模型再到输出源码的流水线。

### 生成流程 - 从字段特性到完整运行时骨架

当前生成器的工作流程可以拆成五步：

1. 语法筛选：只看 `partial class`
2. 粗筛标记：检查类或字段上是否出现 `[DotPudicaView]`、`[BindTo]`、`[BindCommand]`
3. 语义分析：解析真实符号、ViewModel 类型、字段类型与特性参数
4. 中间建模：转成 `ViewClassInfo`、`PropertyBindingInfo`、`CommandBindingInfo`
5. 代码输出：生成 `*.Bindings.g.cs`

它的本质不是直接拼字符串，而是先把 View 的绑定意图提炼成一套中间模型，再由代码生成阶段统一输出。

### 第一步：语法层粗筛 - 先找到可能相关的类

`IsRelevantClass` 的职责是尽量快地排除无关类型。当前筛选规则是：

- 节点必须是 `ClassDeclarationSyntax`
- 类必须是 `partial`
- 类上存在 `[DotPudicaView]`
- 或字段上存在 `[BindTo]` / `[BindCommand]`

这一层只做字符串级判断，不做昂贵的符号解析。它相当于先把“候选样本”挑出来，避免后续语义分析浪费在完全无关的类上。

### 第二步：语义层解析 - 把声明变成准确的结构信息

进入 `TransformClassDeclaration` 后，生成器开始使用 `SemanticModel` 和 `INamedTypeSymbol` 做真正的语义推导。

它会解析出：

- 当前类的命名空间
- 类名
- 绑定使用的 ViewModel 类型
- 是否已经手写 `_Ready()`
- 是否已经手写 `_ExitTree()`
- 该类是否自己声明了 `[DotPudicaView]`
- 所有属性绑定字段
- 所有命令绑定字段

这里有一个很重要的设计：生成器不仅支持“当前类直接声明 `[DotPudicaView]`”，还支持向上查找基类链，继承祖先 View 的 ViewModel 类型。

对应逻辑体现在：

- `GetOwnAttributeViewModelTypeName`
- `GetInheritedAttributeViewModelTypeName`

这意味着框架支持一种很实用的继承模式：

- 基类 View 定义通用运行时骨架
- 子类 View 只补充局部绑定

### 第三步：中间模型 - 先整理结构，再统一出代码

生成器没有把所有信息直接在遍历过程中边读边写，而是先落入几个中间模型：

- `ViewClassInfo`
- `PropertyBindingInfo`
- `CommandBindingInfo`

这几个模型的价值很大：

- 让语义分析与代码输出解耦
- 方便后续扩展新的绑定类型，比如 `[BindItems]`
- 使生成逻辑更易维护，而不是堆叠大量字符串拼接分支

例如 `PropertyBindingInfo` 中会保存：

- `FieldName`
- `ControlType`
- `SourcePath`
- `BindingMode`
- `TargetProperty`
- `SourceEvent`
- `ConverterType`

这就像先把 View 的“电路图”整理成结构化图纸，再交给输出阶段统一施工。

### 第四步：绑定参数推断 - 把简写声明补全为完整配置

`ParseBindToAttribute` 是生成器很重要的一层智能补全逻辑。它会根据特性参数和控件类型自动推断缺省值。

例如：

- 读取 `[BindTo("Username")]` 中的路径
- 解析 `Mode`
- 解析 `TargetProperty`
- 解析 `SourceEvent`
- 解析 `Converter`

如果开发者没有显式填写 `TargetProperty` 和 `SourceEvent`，生成器会去查 `Constants.ControlDefaults`，根据控件类型自动推断。

例如：

- `LineEdit` 默认推断为 `Text` + `text_changed`
- `CheckBox` 默认推断为 `ButtonPressed` + `toggled`
- `ProgressBar` 默认推断为 `Value`，没有反向输入信号

然后再根据是否存在变化信号推断默认绑定模式：

- 有输入信号则默认 `TwoWay`
- 没有输入信号则默认 `OneWay`

这一步让开发者在绝大多数情况下只写最短声明，生成器负责把省略的信息补齐。

### 第五步：输出代码 - 生成部分类的隐藏半边

真正的源码输出发生在：

```csharp
GenerateBindingCode(...)
GenerateClassSource(...)
```

这里会为每个命中的 View 生成一个 `ClassName.Bindings.g.cs` 文件。生成结果本质上是原类的另一半 `partial class`。

如果当前类自己拥有 `[DotPudicaView]`，生成器会注入完整运行时骨架：

- `DotPudicaViewRuntime<TViewModel> __dotPudicaView`
- `BindingContext` 属性
- `ViewModel` 属性
- `DotPudicaInitialize()`
- `DotPudicaDispose()`
- 需要时自动生成 `_Ready()`
- 需要时自动生成 `_ExitTree()`
- `__DotPudicaInitializeBindingsCore()`

如果当前类只是继承自一个已声明 `[DotPudicaView]` 的基类，那么生成器不会重复注入运行时宿主，而是只生成：

- `protected override void __DotPudicaInitializeBindingsCore()`

并在其中先调用：

```csharp
base.__DotPudicaInitializeBindingsCore();
```

再补充当前类新增的绑定语句。

这种设计避免了派生类重复持有多个运行时宿主，也为 View 继承体系保留了扩展空间。

### 生成结果长什么样 - 开发者声明与编译器产物的对应关系

假设开发者写下：

```csharp
[DotPudicaView(typeof(LoginViewModel))]
public partial class LoginView : Control
{
    [Export, BindTo("Username", Mode = BindingMode.TwoWay)]
    private LineEdit _usernameInput = null!;

    [Export, BindCommand("LoginCommand")]
    private Button _loginButton = null!;
}
```

生成器会产出与下面逻辑等价的代码：

```csharp
public partial class LoginView
{
    protected readonly DotPudicaViewRuntime<LoginViewModel> __dotPudicaView = new();

    public BindingContext BindingContext => __dotPudicaView.BindingContext;

    public LoginViewModel? ViewModel
    {
        get => __dotPudicaView.ViewModel;
        set => __dotPudicaView.ViewModel = value;
    }

    protected void DotPudicaInitialize()
    {
        __DotPudicaInitializeBindingsCore();
    }

    protected void DotPudicaDispose()
    {
        __dotPudicaView.Dispose();
    }

    protected virtual void __DotPudicaInitializeBindingsCore()
    {
        __dotPudicaView.BindProperty(
            _usernameInput,
            "Text",
            "text_changed",
            "Username",
            DotPudica.Core.Binding.BindingMode.TwoWay);

        __dotPudicaView.BindCommand(
            _loginButton,
            "pressed",
            "LoginCommand",
            null);
    }
}
```

这就是 DotPudica 的核心思想：开发者写的是简洁声明，编译器生成的是完整机械结构。

### 与运行时的协作关系 - 编译期不是终点，而是接力点

Source Generator 本身并不直接完成绑定，它做的是“把绑定调用组织好”，真正的绑定发生在运行时。

生成代码最终会调用：

- `DotPudicaViewRuntime<TViewModel>.BindProperty(...)`
- `DotPudicaViewRuntime<TViewModel>.BindCommand(...)`

运行时再继续把请求分发到：

- `BindingContext`
- `PropertyBinding`
- `CommandBinding`
- `GodotTargetProxyFactory`
- 各类 `ITargetProxy`

也就是说：

- 编译期负责搭框架和接线图
- 运行时负责真正通电

这种职责分离非常重要，因为它让框架既拥有声明式开发体验，又不需要把所有逻辑塞进生成器内部。

### 为什么不全靠运行时反射 - 编译期方案的工程收益

如果不使用 Source Generator，框架也可以走一条更传统的路：

- 运行时扫描所有字段
- 读取特性
- 动态建立绑定

但这样会带来明显问题：

- 场景进入时要做额外扫描与解析
- 绑定错误更晚暴露
- 生命周期样板仍要开发者手写
- 继承场景与默认推断逻辑更容易散落在多处

DotPudica 选择编译期生成，是希望把这些问题尽量前移：

- 把样板消灭在编译阶段
- 把声明解析前移到 Roslyn 语义层
- 把运行时重点留给真正的数据同步与控件交互

### 当前生成器的实现边界 - 已有能力与下一步空间

当前生成器已经完成：

- `[DotPudicaView]` 类级识别
- `[BindTo]` 属性绑定生成
- `[BindCommand]` 命令绑定生成
- 控件默认属性与信号推断
- 继承链上的 ViewModel 类型追踪
- `_Ready()` / `_ExitTree()` 自动兜底

当前尚未接入的内容包括：

- `[BindItems]` 集合绑定生成
- 更细粒度的诊断信息输出
- 更丰富的生成期错误提示
- 更复杂的模板化列表场景

DotPudica 的 Source Generator 已经具备搭建单值绑定和命令绑定框架骨架的完整能力，但集合绑定与开发者诊断体验仍有待后续更新完善。

你恍然发现：“原来我也是一个 ~~⚪神~~ 源生高手”

## 运行时绑定层 - 看不见的齿轮组

### `BindingContext`&#x20;

`BindingContext` 负责：

- 持有当前 `DataContext`
- 管理 `PropertyBinding` 与 `CommandBinding`
- 在 `DataContext` 切换时自动解绑旧对象并重绑新对象

### `PropertyBinding`&#x20;

`PropertyBinding` 做了几件关键的事：

- 使用 `BindingPath` 读取嵌套路径
- 监听源对象变化
- 在 `TwoWay` 模式下监听控件输入
- 使用 `_isUpdating` 防止循环更新
- 支持 `IValueConverter`

### `CommandBinding`

`CommandBinding` 把 Button 的 `pressed` 这类 Godot 信号翻译成 `ICommand.Execute()`，并支持：

- `CanExecute`
- `ParameterPath`
- 数据上下文切换后的重新订阅

### `ITargetProxy` 与控件代理 - 插头与插座

Godot 控件之间的属性和信号差异很大，DotPudica 通过 `ITargetProxy` 统一适配：

- `LabelProxy`
- `LineEditProxy`
- `TextEditProxy`
- `CheckBoxProxy`
- `SpinBoxProxy`
- `SliderProxy`
- `OptionButtonProxy`
- `ProgressBarProxy`
- `TextureRectProxy`
- `ReflectionProxy`

## 反射代理 - 不只是“害羞”

`ReflectionProxy` 是框架里很实用的保险机制。当某个控件没有专门代理时，框架仍可通过反射读写属性并监听指定信号。

示例 `ReflectionProxySampleView` 展示了这种扩展方式：

```csharp
[BindTo(nameof(ReflectionProxySampleViewModel.SampleText),
    Mode = BindingMode.TwoWay,
    TargetProperty = nameof(ReflectionProbeControl.ValueText),
    SourceEvent = nameof(ReflectionProbeControl.ValueTextChanged))]
private ReflectionProbeControl _probe = null!;
```

## 应用上下文与服务注入 - 含羞草的物语

`AppContext` 与 `ServiceLocator` 提供应用级初始化入口：

```csharp
using DotPudica.Godot;
using Microsoft.Extensions.DependencyInjection;

public partial class GameRoot : Node
{
    private AppContext? _app;

    public override void _Ready()
    {
        _app = new AppContext().Initialize(services =>
        {
            services.AddSingleton<IInventoryService, InventoryService>();
            services.AddTransient<LoginViewModel>();
        });
    }

    public override void _ExitTree()
    {
        _app?.Dispose();
    }
}
```

## 日志与消息总线 - 全局神经信号

### 日志

`LogManager` 提供统一日志入口：

- 编辑器外可退回 `ConsoleLog`
- Godot 环境中可切换到 `GodotLogFactory`
- ViewModel 不需要直接依赖 `GD.Print`

### 消息总线

`MessageBus` 基于 CommunityToolkit 的 `Messenger`，提供：

- 默认弱引用消息总线，降低内存泄漏风险
- 全局通知型通信
- UI 层之间的低耦合解耦

## 示例场景 - `DotPudicaSamples`

&#x20;

### 登录界面 - 表单与异步交互

位置：`DotPudicaSamples/LoginScreen`

验证目标：

- `LineEdit <-> ViewModel` 双向绑定
- `Label <- ViewModel` 单向错误提示
- `ProgressBar <- bool` 转换器绑定
- `Button -> ICommand` 命令绑定
- 异步命令状态切换
- 登录成功后的消息广播

### 设置面板 - 参数面板

位置：`DotPudicaSamples/SettingsPanel`

验证目标：

- `HSlider` 双向绑定数值
- `CheckBox` 双向绑定布尔值
- `OptionButton` 双向绑定索引
- 计算属性联动刷新
- `SaveCommand` 执行与消息分发

### HUD 面板 - 实时数值表现

位置：`DotPudicaSamples/HUD`

验证目标：

- 单向数据展示
- 计算属性驱动 UI
- 输入触发 ViewModel 状态变化
- 玩家死亡消息广播

### ReflectionProxy 样例 - 扩展控件

位置：`DotPudicaSamples/ReflectionProxy`

验证目标：

- 自定义控件反射绑定
- `DotPudicaDispose()` 后解绑有效性
- 多轮创建销毁下的稳定性
- 粗粒度内存漂移观测

### Inventory MVVM Test - 多视图共享状态

位置：`DotPudicaSamples/InventoryMvvmTest`

验证目标：

- 两个 View 共享同一 `InventoryTestViewModel`
- `BindingContext.DataContextChanged` 驱动重绑
- `ObservableCollection` 变化驱动 UI 重绘
- 拖拽、放置校验、增删同步

## 核心脚本说明：

| 文件                                                                 | 作用                     |
| ------------------------------------------------------------------ | ---------------------- |
| `addons/dot-pudica/Core/ViewModels/ViewModelBase.cs`               | ViewModel 生命周期、日志、消息基类 |
| `addons/dot-pudica/Core/ViewModels/ValidatableViewModelBase.cs`    | 表单校验型 ViewModel 基类     |
| `addons/dot-pudica/Core/Binding/BindingContext.cs`                 | 管理 DataContext 与绑定生命周期 |
| `addons/dot-pudica/Core/Binding/PropertyBinding.cs`                | 属性绑定与命令绑定核心实现          |
| `addons/dot-pudica/Godot/Views/DotPudicaViewRuntime.cs`            | View 运行时宿主，承接生成代码      |
| `addons/dot-pudica/Godot/Binding/GodotTargetProxyFactory.cs`       | 控件代理工厂与反射兜底入口          |
| `addons/dot-pudica/Godot/Binding/ControlProxies/ControlProxies.cs` | 常见 Godot 控件代理          |
| `addons/dot-pudica/SourceGenerator/BindingGenerator.cs`            | 编译期生成绑定骨架              |
| `addons/dot-pudica/SourceGenerator/Constants.cs`                   | 控件默认属性与信号映射表           |
| `addons/dot-pudica/Godot/AppContext.cs`                            | 框架初始化、日志切换、DI 接入       |

## 技术亮点 - 把繁琐驯化成顺手的工具

### 1. 编译期生成替代运行时拼接

DotPudica 更偏向“编译期预装配”，让 View 保持声明式，同时避免每次进入场景都重复扫描结构。

### 2. 控件代理隔离引擎差异

通过 `ITargetProxy`，Godot 控件不一致的属性名与信号名被隔离在适配层，不会污染核心绑定模型。

### 3. 自定义控件拥有平滑扩展路径

即使没有专用代理，也可先通过 `ReflectionProxy` 跑通，再视需要落为高性能专用代理。

### 4. 与 CommunityToolkit.Mvvm 深度贴合

开发者可以继续使用熟悉的：

- `[ObservableProperty]`
- `[RelayCommand]`
- `Messenger`
- `Ioc`

### 5. 示例驱动设计

当前样例覆盖了登录、设置、HUD、共享库存、自定义控件压力测试等多个典型 UI 场景，让框架不是停在 API 层，而是落到真实交互问题上。

## 当前限制 - 原型机与量产机的距离

为了让您的开发预期更准确，这里明确列出当前限制：

- `[BindItems]` 已定义但尚未接入生成器，集合界面还需手动维护
- 当前测试以示例场景和手工验证为主，缺少独立测试项目
- 某些复杂控件仍依赖 `ReflectionProxy`
- 生成器的诊断体验仍有继续增强空间

## 未来演进 - 含羞草有一个大大的梦想

我在 [Cholopol-Tetris-Inventory-System](https://github.com/Cholopol/Cholopol-Tetris-Inventory-System.git) 的开发实践中已验证了MVVM在面对背包系统这类数据驱动的复合视图时的合理性与易用性。而在游戏开发领域，传统OOP的MVVM并不万能，以Viewmodel来说，一个物品可能会包含很多无关字段，比如一个不可堆叠的物品却持有堆叠相关字段，导致Viewmodel不可避免地成为了一个胖对象，对CPU访问不友好，在内存性能敏感的场景下更不可接受。由此而导致的cache miss在面对在大量的物品实例时会造成一定程度的性能损失。具体如整理背包功能，要实现这个功能很简单，现成的算法，AI一句话的事，在MVVM架构中就涉及了大量的事件调用。在一些MVVM框架的数据绑定所依赖的表达式树的初始化时会造成瞬时较高的性能压力、批量修改状态会造成很大的GC压力。在Cholopol-Tetris-Inventory-System的实践中一开始我也面临“组件”VS“对象”的取舍，查表法的InventoryTreeCache仍然是OOP思想的一种体现，分散的引用对象是对MVVM的一种妥协，由此造成的运行时、缓存与持久化数据的数据同步问题远比性能问题更为突出。

如果我们分析运行时性能会发现其实性能并不是一个问题，但上层的代码管理则是一个明显的问题。我们不应该依靠上层而要从基础架构出发来解决这一问题。MVVM很成熟，它的数据绑定方式是UI系统的通解，但游戏开发中涉及到大量数据驱动的动态复合视图，这时一点小问题也会在开发中以及运行时导致更大的问题。

不要兼容的妥协，而是要原生的优雅。DotPudica采用更现代的源生成器技术来规避反射/表达式树，并进一步追求面向数据的混合架构，它诞生于追求硬核体验的类塔科夫背包系统的仿制实现，并且也将致力于实现硬核的游戏UI架构。

后续最值得继续推进的方向包括：

- 不止于MVVM，而是基于Godot版的 Cholopol-Tetris-Inventory-System 开发实践持续探索响应式游戏UI混合架构
- 完整落地 `[BindItems]` 的集合模板绑定
- 为更多 Godot 控件提供专用代理，适配 gdscript 编写View视图层
- 强化窗口管理和导航体系
- 提升生成器诊断、错误提示与开发期可观测性
- 敏感的性能追求

## 贡献指南

DotPudica 欢迎任何形式的贡献，包括但不限于 bug 修复、功能扩展、文档完善、示例丰富。

### 提交流类型

| 类型            | 说明                 |
| ------------- | ------------------ |
| Bug Fix       | 修复框架中的错误行为         |
| New Feature   | 新增功能或特性            |
| Control Proxy | 为更多 Godot 控件添加专用代理 |
| Example       | 添加新的示例场景           |
| Documentation | 文档、注释、README 完善    |
| Performance   | 性能优化相关改进           |

### 开发环境要求

- Godot 4.6+（.NET 集成版）
- .NET 8.0 SDK
- Visual Studio 2022 / Rider 或任意支持 C# 12 的编辑器

### 开发流程

**1. Fork & Clone**

```bash
git clone https://github.com/YOUR_USERNAME/dot-pudica-framework.git
cd dot-pudica-framework
git checkout -b feature/your-feature-name
```

**2. 创建示例场景（可选）**

新功能强烈建议附带可运行的示例：

```text
DotPudicaSamples/YourFeatureName/
├── YourView.cs
├── YourView.cs.uid
├── YourViewModel.cs
└── YourViewModel.cs.uid
```

参考现有示例的目录结构和代码风格。

**3. 运行验证**

- 使用 Godot 打开项目，运行 `DotPudicaSamples/test.tscn` 确认新示例正常展示
- 确保 `dotnet build` 编译通过，无警告

**4. 提交规范**

```text
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

类型前缀：

| 类型         | 使用场景        |
| ---------- | ----------- |
| `feat`     | 新功能         |
| `fix`      | bug 修复      |
| `docs`     | 文档改动        |
| `style`    | 代码格式（不影响功能） |
| `refactor` | 重构          |
| `perf`     | 性能优化        |
| `test`     | 测试相关        |
| `chore`    | 构建/工具变更     |

示例：

```text
feat(binditems): add ObservableCollection change detection

- implement CollectionChanged handler in InventoryPanelView
- add CreateItemView / RemoveItemView template methods
- close #12
```

**5. Pull Request 注意事项**

- PR 描述清晰说明改动目的和影响范围
- 关联相关 issue（如果有）
- 确保所有现有示例仍能正常编译运行
- 新增功能尽量附带场景测试或集成自动化测试

### 代码风格约定

- C# 代码遵循 `.editorconfig` 配置
- View 层代码风格参考现有示例，命名清晰、分段合理
- ViewModel 遵循基类约定
- 公共 API 添加 XML 文档注释
- 避免在核心层引入 Godot 特定依赖，保持 Core 层平台无关

### 框架架构共识

贡献代码前请理解框架的核心设计原则：

1. **Core 层平台无关**：`addons/dot-pudica/Core` 不应引用任何 Godot 类型
2. **Godot 适配层隔离**：`addons/dot-pudica/Godot` 负责所有平台特定逻辑
3. **源码生成优先**：能通过编译期生成的代码，不留到运行时
4. **显式优于隐式**：生命周期和资源清理必须清晰可见
5. **示例即文档**：新功能必须有配套可运行的示例场景

### 讨论渠道

- GitHub Issues：功能讨论、bug 报告
- GitHub Discussions：架构设计、方向规划

### 许可证

本项目基于 MIT 许可证开源。贡献代码即表示您同意您的代码将按照同一许可证发布。
