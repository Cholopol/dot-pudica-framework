# DotPudica Framework - A Data-Driven Framework for Godot .NET

<div align="center">

<img src=".github/banner.png" alt="DotPudica Preview" width="50%"/>

</div>

!\[Godot]\(https\://img.shields.io/badge/Godot-4.6+-478CBF?style=flat-square\&logo=godotengine\&logoColor=white null) !\[.NET]\(https\://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square\&logo=dotnet\&logoColor=white null) !\[C#]\(https\://img.shields.io/badge/C%23-12-239120?style=flat-square\&logo=csharp\&logoColor=white null) !\[MVVM]\(https\://img.shields.io/badge/Architecture-MVVM-0A7E8C?style=flat-square null) !\[Source Generator]\(https\://img.shields.io/badge/Roslyn-Source\_Generator-CB4B16?style=flat-square null) !\[Platform]\(https\://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux%20%7C%20Android%20%7C%20Web-6C757D?style=flat-square null) !\[License]\(https\://img.shields.io/badge/License-MIT-green?style=flat-square null) !\[Status]\(https\://img.shields.io/badge/Status-Prototype-orange?style=flat-square null) [!\[Stars\](https://img.shields.io/github/stars/dot-pudica/dot-pudica-framework?style=flat-square\&logo=github\&color=yellow null)](https://github.com/dot-pudica/dot-pudica-framework/stargazers) [!\[Forks\](https://img.shields.io/github/forks/dot-pudica/dot-pudica-framework?style=flat-square\&logo=github null)](https://github.com/dot-pudica/dot-pudica-framework/network/members)

[简体中文](README_CN.md) | English

DotPudica is a lightweight MVVM framework for Godot 4.6 + .NET 8. It migrates the traditional .NET UI binding model to Godot's node-based interface system, letting View handle controls and presentation, while ViewModel handles state, commands, messaging, and business workflows. Committed to achieving a seamless and convenient game UI development experience.

The DotPudica repository includes:

- A set of example scenes that can be verified directly in Godot
- A runnable MVVM framework infrastructure:

| Project                             | Purpose                              | Keywords                                              |
| ----------------------------------- | ------------------------------------ | ----------------------------------------------------- |
| `DotPudicaFramework`                | Godot main project and example entry | `project.godot`, example scenes, debug carrier        |
| `addons/dot-pudica/Core`            | Engine-agnostic MVVM core layer      | `BindingContext`, `PropertyBinding`, `ViewModelBase`  |
| `addons/dot-pudica/Godot`           | Godot adaptation layer               | `DotPudicaViewRuntime`, control proxies, log bridging |
| `addons/dot-pudica/SourceGenerator` | Roslyn compile-time generation layer | `[DotPudicaView]`, `[BindTo]`, `[BindCommand]`        |

The runtime pipeline can be summarized as:

```text
Godot Control / Node
    ↓ Mount controls via [Export]
Partial View Stub
    ↓ Declare intent via [DotPudicaView] / [BindTo] / [BindCommand]
Source Generator
    ↓ Generate ViewModel, BindingContext, initialization and binding code
DotPudicaViewRuntime<TViewModel>
    ↓ Establish BindingContext
PropertyBinding / CommandBinding
    ↓ Interact with Godot controls via ITargetProxy
ViewModel / ICommand / Messenger / Services
```

The value of this layering:

- Core binding logic does not directly depend on specific scene structures
- Godot control differences are absorbed by the `ITargetProxy` layer
- View boilerplate code is absorbed by Source Generator
- ViewModel can still use CommunityToolkit.Mvvm's mature capabilities

## Tech Stack

This project is currently built on the following technology combination:

- `Godot.NET.Sdk/4.6.1`
- .NET 8 main runtime, Android target reserves `net9.0`
- `CommunityToolkit.Mvvm` for `ObservableProperty`, `RelayCommand`, `Messenger`, `Ioc`
- `Microsoft.Extensions.DependencyInjection` for service container
- Roslyn Incremental Source Generator for auto-generating View binding boilerplate code

The advantages of this combination are direct:

- ViewModel writing style is close to standard .NET MVVM, no extra DSL burden
- Binding declarations are close to control fields, highly readable
- Generator completes boilerplate assembly at compile time, avoiding runtime structure scanning
- Runtime still retains reflection fallback capability, balancing extensibility

## Quick Start - Let UI Bloom Like a Mimosa

### 1. Open the Project

- Open `project.godot` in the root directory using Godot 4.6.1
- Ensure the corresponding .NET SDK is installed on your machine

### 2. Compile the Project

Godot .NET projects have their .NET compilation managed by the Godot editor, no need to manually run `dotnet build`.

**Method 1: Compile in Editor (Recommended)**

- After opening the project with Godot 4.6.1, press `Ctrl + Shift + B` or click the hammer icon 🔨 **Build** to trigger compilation
- Godot will automatically recognize the reference relationships of the three addon projects `DotPudica.Core`, `DotPudica.Godot`, `DotPudica.SourceGenerator` and compile them in the correct order

**Method 2: Command Line Compilation**

```bash
dotnet build DotPudicaFramework.sln
```

**Method 3: Full Compilation on Export**

- When selecting a target platform (Windows/macOS/Linux/Android/Web) for export in **Project → Export**, Godot will execute a full compilation, ensuring all dependencies and source generator outputs are included in the final product

### 3. Write a Minimal View

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

### 4. Write the Corresponding ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;

public partial class MyPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Hello DotPudica";
}
```

## ViewModel Base Classes - Internal Skills and Breathing Techniques

### `ViewModelBase` - Logging, Messaging, and Lifecycle

`ViewModelBase` inherits from `ObservableObject` and includes:

- Lazily loaded log object `Log`
- Weak reference message bus `Messenger`
- `Send<TMessage>()` and `Register<TMessage>()` shortcut methods
- `Dispose()` lifecycle cleanup entry

Recommended template:

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

### `ValidatableViewModelBase` - Data Annotation Validation and Form Validation

`ValidatableViewModelBase` inherits from `ObservableValidator`, suitable for form-type interfaces:

- Supports built-in validation annotations like `[Required]`, `[Range]`, `[EmailAddress]`
- Provides `ValidateAll()` unified validation entry
- Suitable for registration, character creation, settings pages, and other scenarios requiring input constraints

Recommended template:

```csharp
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

public partial class RegistrationViewModel : ValidatableViewModelBase
{
    [ObservableProperty]
    [Required(ErrorMessage = "Username cannot be empty")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    private string _username = "";

    [RelayCommand]
    private void Submit()
    {
        if (!ValidateAll())
            return;
    }
}
```

## Data Binding - From "Touch" to "Pudica"

### Implemented Binding Methods

The framework currently has two main pipelines implemented:

| Binding Type     | Entry Attribute | Description                                             |
| ---------------- | --------------- | ------------------------------------------------------- |
| Property Binding | `[BindTo]`      | Connects control properties to ViewModel property paths |
| Command Binding  | `[BindCommand]` | Binds control events to `ICommand`                      |

### Binding Modes

`[BindTo]` supports the following modes:

| Mode             | Direction                   | Typical Controls                                            |
| ---------------- | --------------------------- | ----------------------------------------------------------- |
| `OneWay`         | ViewModel -> View           | `Label`, `ProgressBar`                                      |
| `TwoWay`         | ViewModel <-> View          | `LineEdit`, `CheckBox`, `Slider`                            |
| `OneWayToSource` | View -> ViewModel           | Input-dominated scenarios                                   |
| `OneTime`        | No updates after first sync | Static display                                              |
| `Default`        | Auto inference              | If input signal exists, infers `TwoWay`, otherwise `OneWay` |

### Supported Default Control Inference

The generator has built-in inference rules for common controls in `Constants.ControlDefaults`, for example:

- `LineEdit -> Text / text_changed`
- `TextEdit -> Text / text_changed`
- `SpinBox -> Value / value_changed`
- `CheckBox -> ButtonPressed / toggled`
- `OptionButton -> Selected / item_selected`
- `ProgressBar -> Value`
- `Label -> Text`
- `Button -> pressed`

Often just writing:

```csharp
[Export, BindTo("Username")]
private LineEdit _usernameInput = null!;
```

The generator can automatically infer the target property and change signal.

### Value Converters

When ViewModel value types don't exactly match control expression, you can plug in a converter via `Converter`:

```csharp
[Export, BindTo("IsLoading", Mode = BindingMode.OneWay,
    Converter = typeof(BoolToVisibilityConverter))]
private ProgressBar _loadingBar = null!;
```

Applicable scenarios include:

- Boolean to visibility
- Enum to text
- Numeric to style or color
- Data model to Godot resource objects

### Command Binding

Command binding connects Godot control events to `ICommand`:

```csharp
[Export, BindCommand("LoginCommand")]
private Button _loginButton = null!;
```

In ViewModel, just:

```csharp
[RelayCommand]
private void Login()
{
    // Business logic
}
```

## View Layer Writing - The Control Panel for UI Supervision

A typical View usually only does four things:

1. Mount Godot controls with `[Export]`
2. Declare bindings with `[BindTo]` / `[BindCommand]`
3. Create or inject `ViewModel` in `_Ready()`
4. Call `DotPudicaDispose()` in `_ExitTree()`

Recommended template:

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

## ViewModel Layer Writing - Engine-Agnostic UI Dispatcher

ViewModel's responsibility is not to control the node tree, but to describe state changes and business intent:

- Leave Godot scene structure in View
- Leave testable business state in ViewModel
- Combine interface presentation through `ObservableProperty` and computed properties
- Organize interaction actions through `RelayCommand`
- Push cross-module messages through `Send()` or `MessageBus`

Recommended template:

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

## Source Generation - YuanSheng, Start!

`addons/dot-pudica/SourceGenerator/BindingGenerator.cs` is the key to DotPudica's development experience. It's not just a helper script to help you write less code, but a compile-time engine that translates View's declarative style into runnable binding infrastructure.

### Design Goals - Make Boilerplate Disappear at Compile Time

DotPudica's Source Generator mainly solves three problems:

1. Avoid developers repeatedly writing `BindingContext`, `ViewModel` properties, and initialization code in every View
2. Translate "binding declarations on control fields" into actual runtime calls
3. Inject the runtime skeleton needed for MVVM while preserving Godot's native View writing style

In other words, developers write "declarations", and the generator fills in "structure" and "wiring".

### Technology Choice - Why Use Incremental Generator

DotPudica chose Roslyn `IIncrementalGenerator` instead of the old-style one-time Generator. The reason is that incremental generators are better suited for UI framework development scenarios with frequent editing and compilation.

The benefits it brings include:

- Only process relevant syntax nodes, avoiding full project scanning
- Support syntax filtering and semantic analysis in stages
- Designed for continuous incremental builds in large solutions
- More friendly to instant feedback in IDE

In implementation, the generator entry is:

```csharp
[Generator]
internal sealed class BindingGenerator : IIncrementalGenerator
```

In the initialization phase, through:

```csharp
context.SyntaxProvider.CreateSyntaxProvider(...)
```

Build a pipeline from syntax nodes to binding models to output source code.

### Generation Process - From Field Attributes to Complete Runtime Skeleton

The current generator's workflow can be broken into five steps:

1. Syntax filtering: Only look at `partial class`
2. Coarse filtering: Check if `[DotPudicaView]`, `[BindTo]`, `[BindCommand]` appear on class or fields
3. Semantic analysis: Parse real symbols, ViewModel types, field types, and attribute parameters
4. Intermediate modeling: Convert to `ViewClassInfo`, `PropertyBindingInfo`, `CommandBindingInfo`
5. Code output: Generate `*.Bindings.g.cs`

Its essence is not directly concatenating strings, but first extracting View's binding intent into a set of intermediate models, then unified output in the code generation phase.

### Step 1: Syntax Layer Coarse Filtering - First Find Potentially Relevant Classes

`IsRelevantClass`'s responsibility is to exclude irrelevant types as quickly as possible. Current filtering rules are:

- Node must be `ClassDeclarationSyntax`
- Class must be `partial`
- Class has `[DotPudicaView]`
- Or fields have `[BindTo]` / `[BindCommand]`

This layer only does string-level judgment, not expensive symbol resolution. It's like picking out "candidate samples" first, avoiding wasting subsequent semantic analysis on completely irrelevant classes.

### Step 2: Semantic Layer Parsing - Turn Declarations into Accurate Structural Information

After entering `TransformClassDeclaration`, the generator starts using `SemanticModel` and `INamedTypeSymbol` for real semantic deduction.

It will parse:

- Current class's namespace
- Class name
- ViewModel type used for binding
- Whether `_Ready()` is already handwritten
- Whether `_ExitTree()` is already handwritten
- Whether this class declared `[DotPudicaView]` itself
- All property binding fields
- All command binding fields

There's a very important design here: the generator not only supports "current class directly declaring `[DotPudicaView]`", but also supports looking up the base class chain to inherit ancestor View's ViewModel type.

Corresponding logic is reflected in:

- `GetOwnAttributeViewModelTypeName`
- `GetInheritedAttributeViewModelTypeName`

This means the framework supports a very practical inheritance pattern:

- Base class View defines common runtime skeleton
- Child class View only supplements local bindings

### Step 3: Intermediate Model - First Organize Structure, Then Unified Code Output

The generator doesn't read and write directly during traversal, but first falls into several intermediate models:

- `ViewClassInfo`
- `PropertyBindingInfo`
- `CommandBindingInfo`

The value of these models is significant:

- Decouples semantic analysis from code output
- Facilitates future extension of new binding types, like `[BindItems]`
- Makes generation logic easier to maintain, instead of piling up lots of string concatenation branches

For example, `PropertyBindingInfo` saves:

- `FieldName`
- `ControlType`
- `SourcePath`
- `BindingMode`
- `TargetProperty`
- `SourceEvent`
- `ConverterType`

This is like first organizing View's "circuit diagram" into structured blueprints, then handing to the output phase for unified construction.

### Step 4: Binding Parameter Inference - Complete Short Declarations into Full Configurations

`ParseBindToAttribute` is a very important layer of intelligent completion logic in the generator. It automatically infers default values based on attribute parameters and control types.

For example:

- Read the path in `[BindTo("Username")]`
- Parse `Mode`
- Parse `TargetProperty`
- Parse `SourceEvent`
- Parse `Converter`

If developer doesn't explicitly fill in `TargetProperty` and `SourceEvent`, the generator will look up `Constants.ControlDefaults` to automatically infer based on control type.

For example:

- `LineEdit` defaults to `Text` + `text_changed`
- `CheckBox` defaults to `ButtonPressed` + `toggled`
- `ProgressBar` defaults to `Value`, no reverse input signal

Then infer default binding mode based on whether change signal exists:

- If input signal exists, default to `TwoWay`
- If no input signal, default to `OneWay`

This step lets developers write only the shortest declaration in most cases, with the generator responsible for filling in omitted information.

### Step 5: Output Code - Generate the Hidden Half of Partial Class

The actual source code output happens in:

```csharp
GenerateBindingCode(...)
GenerateClassSource(...)
```

Here a `ClassName.Bindings.g.cs` file is generated for each matched View. The generated result is essentially the other half `partial class` of the original class.

If the current class owns `[DotPudicaView]` itself, the generator injects the complete runtime skeleton:

- `DotPudicaViewRuntime<TViewModel> __dotPudicaView`
- `BindingContext` property
- `ViewModel` property
- `DotPudicaInitialize()`
- `DotPudicaDispose()`
- Auto-generate `_Ready()` if needed
- Auto-generate `_ExitTree()` if needed
- `__DotPudicaInitializeBindingsCore()`

If the current class only inherits from a base class that already declared `[DotPudicaView]`, the generator won't repeatedly inject runtime host, but only generate:

- `protected override void __DotPudicaInitializeBindingsCore()`

And in it first call:

```csharp
base.__DotPudicaInitializeBindingsCore();
```

Then supplement binding statements added by current class.

This design avoids derived classes repeatedly holding multiple runtime hosts, and also preserves extension space for View inheritance hierarchy.

### What Does Generated Result Look Like - Correspondence Between Developer Declarations and Compiler Output

Assuming developer writes:

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

The generator produces code logically equivalent to:

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

This is DotPudica's core idea: developers write concise declarations, compiler generates complete mechanical structures.

### Collaboration with Runtime - Compile Time is Not the End, But a Relay Point

Source Generator itself doesn't directly complete binding, it "organizes binding calls", and real binding happens at runtime.

Generated code eventually calls:

- `DotPudicaViewRuntime<TViewModel>.BindProperty(...)`
- `DotPudicaViewRuntime<TViewModel>.BindCommand(...)`

Runtime then continues distributing requests to:

- `BindingContext`
- `PropertyBinding`
- `CommandBinding`
- `GodotTargetProxyFactory`
- Various `ITargetProxy` implementations

This separation of responsibilities is important because it gives the framework both declarative development experience and avoids stuffing all logic into the generator.

### Why Not Fully Rely on Runtime Reflection - Engineering Benefits of Compile-Time Approach

Without Source Generator, the framework could take a more traditional path:

- Runtime scanning of all fields
- Reading attributes
- Dynamically establishing bindings

But this brings obvious problems:

- Additional scanning and parsing when entering a scene
- Binding errors exposed later
- Lifecycle boilerplate still requires manual developer work
- Inheritance scenarios and default inference logic can easily scatter across multiple locations

DotPudica chooses compile-time generation to move these issues forward:

- Eliminate boilerplate at compile time
- Move declaration parsing to Roslyn semantic layer
- Focus runtime on real data synchronization and control interaction

### Current Generator Implementation Boundaries - Existing Capabilities and Next Steps

The generator has already completed:

- `[DotPudicaView]` class-level recognition
- `[BindTo]` property binding generation
- `[BindCommand]` command binding generation
- Control default property and signal inference
- ViewModel type tracking on inheritance chain
- `_Ready()` / `_ExitTree()` automatic fallback

Currently not yet connected:

- `[BindItems]` collection binding generation
- Finer-grained diagnostic information output
- Richer compile-time error hints
- More complex templated list scenarios

DotPudica's Source Generator already has complete capability to build single-value binding and command binding framework skeletons, but collection binding and developer diagnostic experience remain to be improved in future updates.

## Runtime Binding Layer - The Invisible Gear Set

### `BindingContext`

`BindingContext` is responsible for:

- Holding current `DataContext`
- Managing `PropertyBinding` and `CommandBinding`
- Automatically unbinding old objects and rebinding new ones when `DataContext` switches

### `PropertyBinding`

`PropertyBinding` does several key things:

- Uses `BindingPath` to read nested paths
- Listens for source object changes
- Listens for control input in `TwoWay` mode
- Uses `_isUpdating` to prevent circular updates
- Supports `IValueConverter`

### `CommandBinding`

`CommandBinding` translates Godot signals like Button's `pressed` into `ICommand.Execute()`, and supports:

- `CanExecute`
- `ParameterPath`
- Resubscription after data context switching

### `ITargetProxy` and Control Proxies - Plugs and Sockets

Godot controls have large differences in properties and signals, and DotPudica unifies adaptation through `ITargetProxy`:

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

## Reflection Proxy - Not Just "Pudica"

`ReflectionProxy` is a practical insurance mechanism in the framework. When a control doesn't have a dedicated proxy, the framework can still read/write properties and listen to specified signals through reflection.

The example `ReflectionProxySampleView` demonstrates this extension approach:

```csharp
[BindTo(nameof(ReflectionProxySampleViewModel.SampleText),
    Mode = BindingMode.TwoWay,
    TargetProperty = nameof(ReflectionProbeControl.ValueText),
    SourceEvent = nameof(ReflectionProbeControl.ValueTextChanged))]
private ReflectionProbeControl _probe = null!;
```

## AppContext and Service Injection - The Story of Mimosa

`AppContext` and `ServiceLocator` provide application-level initialization entry:

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

## Logging and Message Bus - Global Neural Signals

### Logging

`LogManager` provides a unified logging entry:

- Outside editor can fall back to `ConsoleLog`
- In Godot environment can switch to `GodotLogFactory`
- ViewModel doesn't need to directly depend on `GD.Print`

### Message Bus

`MessageBus` is based on CommunityToolkit's `Messenger`, providing:

- Default weak reference message bus, reducing memory leak risk
- Global notification-style communication
- Low-coupling decoupling between UI layers

## Core Scripts - The Pillars of the Framework

| File                                                               | Purpose                                                            |
| ------------------------------------------------------------------ | ------------------------------------------------------------------ |
| `addons/dot-pudica/Core/ViewModels/ViewModelBase.cs`               | ViewModel base class, integrates logging, messaging, and lifecycle |
| `addons/dot-pudica/Core/Binding/BindingContext.cs`                 | Manages DataContext and binding lifecycle                          |
| `addons/dot-pudica/Core/Binding/PropertyBinding.cs`                | Property binding and command binding core implementation           |
| `addons/dot-pudica/Godot/Views/DotPudicaViewRuntime.cs`            | View runtime host, receives generated code                         |
| `addons/dot-pudica/Godot/Binding/GodotTargetProxyFactory.cs`       | Control proxy factory and reflection fallback entry                |
| `addons/dot-pudica/Godot/Binding/ControlProxies/ControlProxies.cs` | Common Godot control proxies                                       |
| `addons/dot-pudica/SourceGenerator/BindingGenerator.cs`            | Compile-time engine for generating binding boilerplate             |
| `addons/dot-pudica/SourceGenerator/Constants.cs`                   | Control default property and signal mapping table                  |
| `addons/dot-pudica/Godot/AppContext.cs`                            | Framework initialization, log switching, DI container integration  |

## Example Scenes - `DotPudicaSamples`

### Login Screen - Form and Async Interaction

Location: `DotPudicaSamples/LoginScreen`

Validation targets:

- `LineEdit <-> ViewModel` two-way binding
- `Label <- ViewModel` one-way error display
- `ProgressBar <- bool` converter binding
- `Button -> ICommand` command binding
- Async command state switching
- Message broadcast after successful login

### Settings Panel - Parameter Panel

Location: `DotPudicaSamples/SettingsPanel`

Validation targets:

- `HSlider` two-way numeric binding
- `CheckBox` two-way boolean binding
- `OptionButton` two-way index binding
- Computed property linkage refresh
- `SaveCommand` execution and message distribution

### HUD Panel - Real-time Value Display

Location: `DotPudicaSamples/HUD`

Validation targets:

- One-way data display
- Computed property driven UI
- Input triggers ViewModel state changes
- Player death message broadcast

### ReflectionProxy Sample - Extended Controls

Location: `DotPudicaSamples/ReflectionProxy`

Validation targets:

- Custom control reflection binding
- `DotPudicaDispose()` unbinding effectiveness after disposal
- Stability under multiple create/destroy cycles
- Coarse-grained memory drift observation

### Inventory MVVM Test - Multi-view Shared State

Location: `DotPudicaSamples/InventoryMvvmTest`

Validation targets:

- Two Views sharing same `InventoryTestViewModel`
- `BindingContext.DataContextChanged` drives rebinding
- `ObservableCollection` changes drive UI redraw
- Drag, drop validation, add/remove sync

## Technical Highlights - Taming Complexity into Handy Tools

### 1. Compile-Time Generation Replaces Runtime Concatenation

DotPudica prefers "compile-time pre-assembly", keeping View declarative while avoiding repeated structure scanning every time entering a scene.

### 2. Control Proxies Isolate Engine Differences

Through `ITargetProxy`, Godot control inconsistent property names and signal names are isolated in the adaptation layer, without polluting the core binding model.

### 3. Custom Controls Have Smooth Extension Path

Even without dedicated proxies, `ReflectionProxy` can be used first to get things working, then converted to high-performance dedicated proxies as needed.

### 4. Deep Integration with CommunityToolkit.Mvvm

Developers can continue using familiar features:

- `[ObservableProperty]`
- `[RelayCommand]`
- `Messenger`
- `Ioc`

### 5. Example-Driven Design

Current examples cover multiple typical UI scenarios including login, settings, HUD, shared inventory, custom control stress testing, making the framework not just stay at API level but land on real interaction problems.

## Current Limitations - The Distance Between Prototype and Production

To set accurate expectations, here are the current limitations explicitly listed:

- `[BindItems]` is defined but not yet connected to the generator, collection interfaces still need manual maintenance
- Current testing is mainly example scenes and manual verification, lacking independent test projects
- Some complex controls still rely on `ReflectionProxy`
- Generator diagnostics experience still has room for enhancement

## Future Evolution - Mimosa Has a Big Dream

In the development practice of [Cholopol-Tetris-Inventory-System](https://github.com/Cholopol/Cholopol-Tetris-Inventory-System.git), I have verified the rationality and usability of MVVM when facing data-driven composite views like inventory systems. In the field of game development, traditional OOP MVVM is not all-powerful. Take ViewModel as an example: an item may contain many irrelevant fields. For example, a non-stackable item holds stack-related fields, making ViewModel inevitably become a fat object, unfriendly to CPU access, and even more unacceptable in memory-performance-sensitive scenarios. The resulting cache miss can cause a certain degree of performance loss when facing a large number of item instances. Specifically, for the inventory sorting function, implementing it is very simple - ready-made algorithms, AI can do it in one sentence. In MVVM architecture, it involves a large number of event calls. In some MVVM frameworks, the initialization of expression trees that data binding depends on can cause instantaneous high performance pressure, and batch state modifications can cause great GC pressure. In the practice of Cholopol-Tetris-Inventory-System, I also faced the choice between "component" vs "object" at the beginning. The lookup-table-based InventoryTreeCache is still an embodiment of OOP thinking, and scattered reference objects are a compromise for MVVM, resulting in runtime, cache and persistent data synchronization problems far more prominent than performance issues.

If we analyze runtime performance, we will find that performance is actually not a problem, but upper-level code management is an obvious problem. We should not rely on the upper level but start from the infrastructure to solve this problem. MVVM is very mature, and its data binding method is a general solution for UI systems. However, game development involves a large number of data-driven dynamic composite views. At this time, small problems will also lead to bigger problems in development and runtime.

Not compatible compromise, but native elegance. DotPudica adopts more modern source generator technology to avoid reflection/expression trees, and further pursues data-oriented hybrid architecture. It was born from the pursuit of hardcore experience like Tarkov-style inventory system replica implementation, and will also be committed to achieving hardcore game UI architecture.

The directions most worth continuing to promote later include:

- Not limited to MVVM, but based on Godot version of Cholopol-Tetris-Inventory-System development practice to continuously explore reactive game UI hybrid architecture
- Fully implement `[BindItems]` collection template binding
- Provide dedicated proxies for more Godot controls, adapting gdscript to write View layer
- Strengthen window management and navigation system
- Improve generator diagnostics, error hints and development-time observability
- Pursuit of sensitive performance

## Contributing Guide

DotPudica welcomes contributions of any kind, including but not limited to bug fixes, feature extensions, documentation improvements, and example enrichment.

### Commit Types

| Type          | Description                                   |
| ------------- | --------------------------------------------- |
| Bug Fix       | Fix erroneous behavior in the framework       |
| New Feature   | Add new functions or features                 |
| Control Proxy | Add dedicated proxies for more Godot controls |
| Example       | Add new example scenes                        |
| Documentation | Documentation, comments, README improvements  |
| Performance   | Performance optimization related improvements |

### Development Environment Requirements

- Godot 4.6+ (.NET integrated version)
- .NET 8.0 SDK
- Visual Studio 2022 / Rider or any editor supporting C# 12

### Development Process

**1. Fork & Clone**

```bash
git clone https://github.com/YOUR_USERNAME/dot-pudica-framework.git
cd dot-pudica-framework
git checkout -b feature/your-feature-name
```

**2. Create Example Scene (Optional)**

New features are strongly recommended to be accompanied by runnable examples:

```
DotPudicaSamples/YourFeatureName/
├── YourView.cs
├── YourView.cs.uid
├── YourViewModel.cs
└── YourViewModel.cs.uid
```

Refer to existing examples for directory structure and code style.

**3. Run Verification**

- Open the project with Godot, run `DotPudicaSamples/test.tscn` to confirm new examples display correctly
- Ensure `dotnet build` compiles without warnings

**4. Commit Convention**

```text
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

Type prefixes:

| Type       | Usage Scenario                     |
| ---------- | ---------------------------------- |
| `feat`     | New feature                        |
| `fix`      | Bug fix                            |
| `docs`     | Documentation changes              |
| `style`    | Code format (no functional impact) |
| `refactor` | Refactoring                        |
| `perf`     | Performance optimization           |
| `test`     | Test related                       |
| `chore`    | Build/tool changes                 |

Example:

```text
feat(binditems): add ObservableCollection change detection

- implement CollectionChanged handler in InventoryPanelView
- add CreateItemView / RemoveItemView template methods
- close #12
```

**5. Pull Request Notes**

- PR description clearly states the purpose and scope of changes
- Link related issues (if any)
- Ensure all existing examples still compile and run correctly
- New features should preferably come with scene tests or integration automated tests

### Code Style Conventions

- C# code follows `.editorconfig` configuration
- View layer code style refers to existing examples, clear naming, reasonable segmentation
- ViewModel follows base class conventions
- Add XML documentation comments for public APIs
- Avoid introducing Godot-specific dependencies in core layer, keep Core layer platform-agnostic

### Framework Architecture Consensus

Please understand the framework's core design principles before contributing code:

1. **Core layer platform-agnostic**: `addons/dot-pudica/Core` should not reference any Godot types
2. **Godot adaptation layer isolation**: `addons/dot-pudica/Godot` is responsible for all platform-specific logic
3. **Source generation first**: Code that can be generated at compile time should not be left to runtime
4. **Explicit over implicit**: Lifecycle and resource cleanup must be clearly visible
5. **Examples as documentation**: New features must have accompanying runnable example scenes

### Discussion Channels

- GitHub Issues: Feature discussion, bug reports
- GitHub Discussions: Architecture design, direction planning

### License

This project is open source under the MIT license. By contributing code, you agree that your code will be released under the same license.
