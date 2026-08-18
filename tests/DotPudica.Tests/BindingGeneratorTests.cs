using DotPudica.Core.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DotPudica.SourceGenerator;

namespace DotPudica.Tests;

public class BindingGeneratorTests
{
    [Fact]
    public void GeneratesUiContextCaptureAndAllBindingKinds()
    {
        const string source = """
            global using System;
            using System.Collections.ObjectModel;
            using System.Windows.Input;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Core.Binding.Attributes;
            using DotPudica.Godot.Views;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }

                public class Label : Node
                {
                    public string Text { get; set; } = "";
                }

                public class Button : Node { }
                public class Container : Node { }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public enum BindingMode { Default, OneWay, TwoWay }
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Core.Binding.Attributes
            {
                public sealed class BindToAttribute(string path) : Attribute
                {
                    public BindingMode Mode { get; set; }
                    public string? Target { get; set; }
                    public string? Signal { get; set; }
                    public Type? Converter { get; set; }
                }

                public sealed class BindCommandAttribute(string commandName) : Attribute
                {
                    public string? Parameter { get; set; }
                    public string? Signal { get; set; }
                }

                public sealed class ItemsSourceAttribute(string path, string itemScene) : Attribute
                {
                    public int PoolSize { get; set; }
                }
            }

            namespace DotPudica.Godot
            {
            }

            namespace DotPudica.Godot.Binding.ControlProxies
            {
                public class LabelProxy
                {
                    public LabelProxy(Label label) { }
                }

                public class VirtualizedItemsControl : global::Godot.Node { }
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public DotPudica.Core.Binding.BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindProperty<TSource, TTarget>(
                        object targetProxy,
                        TypedBindingPath<TViewModel, TSource> sourcePath,
                        DotPudica.Core.Binding.BindingMode mode,
                        object? converter = null,
                        Func<TSource, TTarget>? mapForward = null,
                        Func<TTarget, TSource>? mapBack = null) { }
                    public void BindCommand(
                        object target, string signal,
                        TypedBindingPath<TViewModel, ICommand> commandPath,
                        TypedBindingPath<TViewModel, object?>? parameterPath = null) { }
                    public void BindItems<TCollection>(
                        object target, string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath, int poolSize)
                        where TCollection : class { }
                    public void BindVirtualizedItems<TCollection>(
                        DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                        string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath,
                        Func<TViewModel, System.Windows.Input.ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
                public sealed class SampleViewModel
                {
                    public string Title { get; set; } = "";
                    public ICommand SaveCommand { get; set; } = null!;
                    public ObservableCollection<string> Items { get; } = [];
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [BindTo("Title")]
                    private Label title = null!;

                    [BindCommand("SaveCommand")]
                    private Button saveButton = null!;

                    [ItemsSource("Items", "res://item.tscn", PoolSize = 8)]
                    private Container items = null!;
                }
            }
            """;

        var generated = RunGenerator(source);

        Assert.Contains("__dotPudicaView.CaptureUiContext();", generated);
        Assert.Contains("TypedBindingPath", generated);
        Assert.Contains("LabelProxy", generated);
        Assert.Contains(
            "__dotPudicaView.BindProperty<string, string>(__proxy, __path, DotPudica.Core.Binding.BindingMode.OneWay);",
            generated);
        Assert.Contains("BindCommand", generated);
        Assert.Contains("BindItems", generated);
        Assert.DoesNotContain("System.Linq.Expressions", generated);
        Assert.DoesNotContain("BindProperty(title,", generated);
    }

    [Fact]
    public void CollectionValidationError_DoesNotGenerateItemsBinding()
    {
        const string source = """
            global using System;
            using System.Collections.Generic;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Core.Binding.Attributes;
            using DotPudica.Godot.Views;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }

                public class Container : Node { }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Core.Binding.Attributes
            {
                public sealed class ItemsSourceAttribute(string path, string itemScene) : Attribute { }
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public DotPudica.Core.Binding.BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindItems<TCollection>(
                        object target, string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath, int poolSize)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
                public sealed class SampleViewModel
                {
                    public List<string> Items { get; } = [];
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [ItemsSource("Items", "res://item.tscn")]
                    private Container items = null!;
                }
            }
            """;

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA010"
            && diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain("__dotPudicaView.BindItems(items", generated);
    }

    [Fact]
    public void MissingLifecycleEntryPoint_ReportsDiagnostic()
    {
        const string source = """
            global using System;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Godot.Views;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Godot.Binding.ControlProxies
            {
                public class VirtualizedItemsControl : global::Godot.Node { }
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindVirtualizedItems<TCollection>(
                        DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                        string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath,
                        Func<TViewModel, System.Windows.Input.ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    // Forgot to override _Ready()/_ExitTree() to call InitializeView()/DisposeView().
                }
            }
            """;

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA046"
            && diagnostic.Severity == DiagnosticSeverity.Error);

        var (wiredResult, _) = RunGeneratorWithDiagnostics(source.Replace(
            "// Forgot to override _Ready()/_ExitTree() to call InitializeView()/DisposeView().",
            "public override void _Ready() => InitializeView();\n        public override void _ExitTree() => DisposeView();"));
        Assert.DoesNotContain(wiredResult.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA046");
    }

    [Fact]
    public void AutoInitializeFalse_WithDisposeViewWired_ReportsNoDiagnostic()
    {
        const string source = """
            global using System;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Godot.Views;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Godot.Binding.ControlProxies
            {
                public class VirtualizedItemsControl : global::Godot.Node { }
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute
                {
                    public bool AutoInitialize { get; set; } = true;
                }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindVirtualizedItems<TCollection>(
                        DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                        string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath,
                        Func<TViewModel, System.Windows.Input.ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel), AutoInitialize = false)]
                public partial class SampleView : Node
                {
                    public override void _ExitTree() => DisposeView();
                }
            }
            """;

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Id is "DOTPUDICA046" or "DOTPUDICA040");
    }

    [Fact]
    public void NullableStringSource_ToLabelText_DoesNotReportTypeMismatch()
    {
        var source = TypeCheckHarness("""
                public sealed class SampleViewModel
                {
                    public string? RoomId { get; set; }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [BindTo("RoomId")]
                    private Label room = null!;
                }
            """);

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.DoesNotContain(result.Diagnostics, d => d.Id is "DOTPUDICA032" or "DOTPUDICA034" or "DOTPUDICA035");
        Assert.Contains(
            "__dotPudicaView.BindProperty<string?, string>(__proxy, __path, DotPudica.Core.Binding.BindingMode.OneWay);",
            generated);
        Assert.Contains("new LabelProxy(room)", generated);
        Assert.DoesNotContain("mapForward:", generated);
    }

    [Fact]
    public void TextureRect_BindToWithNullableConverter_EmitsNullableTextureTarget()
    {
        var source = NullableProxyHarness("""
                public sealed class IconKeyToTextureConverter : IValueConverter<string, Texture2D?>
                {
                    public Texture2D? Convert(string value) => null;
                    public string ConvertBack(Texture2D? value) => "";
                }

                public sealed class SampleViewModel
                {
                    public string IconKey { get; set; } = "";
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [BindTo("IconKey", Converter = typeof(IconKeyToTextureConverter))]
                    private TextureRect _icon = null!;
                }
            """);

        var (result, generated, compilationDiagnostics) = RunGeneratorWithCompilationDiagnostics(
            source, nullableContext: NullableContextOptions.Enable);

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilationDiagnostics, d => d.Id == "CS8620");
        Assert.Contains(
            "__dotPudicaView.BindProperty<string, Godot.Texture2D?>(__proxy, __path, DotPudica.Core.Binding.BindingMode.OneWay, converter: new Sample.IconKeyToTextureConverter());",
            generated);
        Assert.Contains("new TextureRectProxy(_icon)", generated);
    }

    [Fact]
    public void Label_BindToText_EmitsNonNullableStringTarget()
    {
        var source = NullableProxyHarness("""
                public sealed class SampleViewModel
                {
                    public string Title { get; set; } = "";
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [BindTo("Title")]
                    private Label title = null!;
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains(
            "__dotPudicaView.BindProperty<string, string>(__proxy, __path, DotPudica.Core.Binding.BindingMode.OneWay);",
            generated);
        Assert.Contains("new LabelProxy(title)", generated);
        Assert.DoesNotContain("BindProperty<string, string?>", generated);
    }

    [Fact]
    public void TextureRect_BindToModulate_UsesDelegateProxyWithoutNullableWarning()
    {
        var source = NullableProxyHarness("""
                public sealed class SampleViewModel
                {
                    public Color Tint { get; set; }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [BindTo("Tint", Target = "Modulate")]
                    private TextureRect icon = null!;
                }
            """);

        var (result, generated, compilationDiagnostics) = RunGeneratorWithCompilationDiagnostics(
            source, nullableContext: NullableContextOptions.Enable);

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilationDiagnostics, d => d.Id is "CS8620" or "CS8600" or "CS8601" or "CS8625");
        Assert.Contains(
            "__dotPudicaView.BindProperty<Godot.Color, Godot.Color>(__proxy, __path, DotPudica.Core.Binding.BindingMode.OneWay);",
            generated);
        Assert.Contains(
            "new DelegateTargetProxy<Godot.TextureRect, Godot.Color>(icon, static c => c.Modulate, static (c, v) => c.Modulate = v, null)",
            generated);
        Assert.DoesNotContain("new TextureRectProxy(icon)", generated);
    }

    [Fact]
    public void ReferenceUpcast_OneWay_EmitsMapForward()
    {
        var source = TypeCheckHarness("""
                public class Resource { }
                public class Texture2D : Resource { }

                public class TextureSlot : Node
                {
                    public Resource? Texture { get; set; }
                }

                public sealed class SampleViewModel
                {
                    public Texture2D Icon { get; set; } = null!;
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [BindTo("Icon", Target = "Texture", Mode = BindingMode.OneWay)]
                    private TextureSlot slot = null!;
                }
            """, includeDelegateProxy: true);

        var generated = RunGenerator(source);

        Assert.Contains(
            "__dotPudicaView.BindProperty<Sample.Texture2D, Sample.Resource?>(__proxy, __path, DotPudica.Core.Binding.BindingMode.OneWay, mapForward: static v => (Sample.Resource?)v);",
            generated);
        Assert.Contains(
            "new DelegateTargetProxy<Sample.TextureSlot, Sample.Resource?>(slot, static c => c.Texture, static (c, v) => c.Texture = v, null)",
            generated);
        Assert.DoesNotContain("mapBack:", generated);
    }

    [Fact]
    public void ReferenceUpcast_TwoWayWithoutConverter_ReportsDiagnostic()
    {
        var source = TypeCheckHarness("""
                public class Resource { }
                public class Texture2D : Resource { }

                public class TextureSlot : Node
                {
                    public Resource? Texture { get; set; }
                }

                public sealed class SampleViewModel
                {
                    public Texture2D Icon { get; set; } = null!;
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [BindTo("Icon", Target = "Texture", Mode = BindingMode.TwoWay, Signal = "changed")]
                    private TextureSlot slot = null!;
                }
            """, includeDelegateProxy: true);

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, d =>
            d.Id == "DOTPUDICA034" && d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain("__dotPudicaView.BindProperty<", generated);
    }

    [Fact]
    public void ValueTypeToObject_ReportsBoxingDiagnostic()
    {
        var source = TypeCheckHarness("""
                public class ObjectHost : Node
                {
                    public object? Value { get; set; }
                }

                public sealed class SampleViewModel
                {
                    public int Score { get; set; }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [BindTo("Score", Target = "Value", Mode = BindingMode.OneWay)]
                    private ObjectHost host = null!;
                }
            """, includeDelegateProxy: true);

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, d =>
            d.Id == "DOTPUDICA035" && d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain("mapForward:", generated);
        Assert.DoesNotContain("__dotPudicaView.BindProperty<", generated);
    }

    [Fact]
    public void NumericIntToDouble_EmitsMapForwardAndMapBackForTwoWay()
    {
        var source = TypeCheckHarness("""
                public class DoubleHost : Node
                {
                    public double Value { get; set; }
                }

                public sealed class SampleViewModel
                {
                    public int Score { get; set; }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [BindTo("Score", Target = "Value", Mode = BindingMode.TwoWay, Signal = "changed")]
                    private DoubleHost host = null!;
                }
            """, includeDelegateProxy: true);

        var generated = RunGenerator(source);

        Assert.Contains(
            "__dotPudicaView.BindProperty<int, double>(__proxy, __path, DotPudica.Core.Binding.BindingMode.TwoWay, mapForward: static v => (double)v, mapBack: static v => (int)v);",
            generated);
        Assert.Contains(
            "new DelegateTargetProxy<Sample.DoubleHost, double>(host, static c => c.Value, static (c, v) => c.Value = v, \"changed\")",
            generated);
    }

    [Fact]
    public void BindTo_ProgressBarMaxValue_UsesProgressBarProxyWithRangeProperty()
    {
        const string source = """
            global using System;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Core.Binding.Attributes;
            using DotPudica.Godot.Views;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }

                public class ProgressBar : Node
                {
                    public double Value { get; set; }
                    public double MaxValue { get; set; }
                    public double Step { get; set; }
                }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public enum BindingMode { Default, OneWay, TwoWay }
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Core.Binding.Attributes
            {
                public sealed class BindToAttribute(string path) : Attribute
                {
                    public BindingMode Mode { get; set; }
                    public string? Target { get; set; }
                    public string? Signal { get; set; }
                    public Type? Converter { get; set; }
                }
            }

            namespace DotPudica.Godot.Binding
            {
                public enum RangeBindingProperty { Value, MinValue, MaxValue }
            }

            namespace DotPudica.Godot.Binding.ControlProxies
            {
                public class ProgressBarProxy
                {
                    public ProgressBarProxy(ProgressBar bar, DotPudica.Godot.Binding.RangeBindingProperty property) { }
                }

                public sealed class DelegateTargetProxy<TControl, TValue>
                {
                    public DelegateTargetProxy(
                        TControl control,
                        Func<TControl, TValue> getter,
                        Action<TControl, TValue>? setter,
                        string? changeSignal) { }
                }

                public class VirtualizedItemsControl : global::Godot.Node { }
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public DotPudica.Core.Binding.BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindProperty<TSource, TTarget>(
                        object targetProxy,
                        TypedBindingPath<TViewModel, TSource> sourcePath,
                        DotPudica.Core.Binding.BindingMode mode,
                        object? converter = null,
                        Func<TSource, TTarget>? mapForward = null,
                        Func<TTarget, TSource>? mapBack = null) { }
                    public void BindVirtualizedItems<TCollection>(
                        DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                        string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath,
                        Func<TViewModel, System.Windows.Input.ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
                public sealed class SampleViewModel
                {
                    public double Hp { get; set; }
                    public double MaxHp { get; set; }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [BindTo("Hp")]
                    private ProgressBar hpBar = null!;

                    [BindTo("MaxHp", Target = "MaxValue")]
                    private ProgressBar hpBarMax = null!;
                }
            }
            """;

        var generated = RunGenerator(source);

        Assert.Contains(
            "new ProgressBarProxy(hpBar, DotPudica.Godot.Binding.RangeBindingProperty.Value)",
            generated);
        Assert.Contains(
            "new ProgressBarProxy(hpBarMax, DotPudica.Godot.Binding.RangeBindingProperty.MaxValue)",
            generated);
        // Correctness depends on GodotRangeBinding value coordination, not on generation order.
    }

    [Fact]
    public void BindTo_TargetNotMatchingBuiltInProxyDefault_FallsBackToDelegateProxy()
    {
        const string source = """
            global using System;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Core.Binding.Attributes;
            using DotPudica.Godot.Views;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }

                public class ProgressBar : Node
                {
                    public double Value { get; set; }
                    public double MaxValue { get; set; }
                    public double Step { get; set; }
                }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public enum BindingMode { Default, OneWay, TwoWay }
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Core.Binding.Attributes
            {
                public sealed class BindToAttribute(string path) : Attribute
                {
                    public BindingMode Mode { get; set; }
                    public string? Target { get; set; }
                    public string? Signal { get; set; }
                    public Type? Converter { get; set; }
                }
            }

            namespace DotPudica.Godot.Binding
            {
                public enum RangeBindingProperty { Value, MinValue, MaxValue }
            }

            namespace DotPudica.Godot.Binding.ControlProxies
            {
                public class ProgressBarProxy
                {
                    public ProgressBarProxy(ProgressBar bar, DotPudica.Godot.Binding.RangeBindingProperty property) { }
                }

                public sealed class DelegateTargetProxy<TControl, TValue>
                {
                    public DelegateTargetProxy(
                        TControl control,
                        Func<TControl, TValue> getter,
                        Action<TControl, TValue>? setter,
                        string? changeSignal) { }
                }

                public class VirtualizedItemsControl : global::Godot.Node { }
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public DotPudica.Core.Binding.BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindProperty<TSource, TTarget>(
                        object targetProxy,
                        TypedBindingPath<TViewModel, TSource> sourcePath,
                        DotPudica.Core.Binding.BindingMode mode,
                        object? converter = null,
                        Func<TSource, TTarget>? mapForward = null,
                        Func<TTarget, TSource>? mapBack = null) { }
                    public void BindVirtualizedItems<TCollection>(
                        DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                        string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath,
                        Func<TViewModel, System.Windows.Input.ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
                public sealed class SampleViewModel
                {
                    public double StepSize { get; set; }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [BindTo("StepSize", Target = "Step")]
                    private ProgressBar bar = null!;
                }
            }
            """;

        var generated = RunGenerator(source);

        // Step is not in BuiltInProxySupportedTargets: fall back to DelegateTargetProxy, write property directly.
        Assert.Contains(
            "new DelegateTargetProxy<Godot.ProgressBar, double>(bar, static c => c.Step, static (c, v) => c.Step = v, null)",
            generated);
        Assert.DoesNotContain("new ProgressBarProxy(bar", generated);
    }

    [Fact]
    public void ItemsSource_ItemCommandMatchingElementType_GeneratesItemCommandGetter()
    {
        const string source = """
            global using System;
            using System.Collections.ObjectModel;
            using System.Windows.Input;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Core.Binding.Attributes;
            using DotPudica.Godot.Views;
            using CommunityToolkit.Mvvm.Input;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }

                public class Container : Node { }
            }

            namespace CommunityToolkit.Mvvm.Input
            {
                public sealed class RelayCommandAttribute : Attribute { }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Core.Binding.Attributes
            {
                public sealed class ItemsSourceAttribute(string path, string itemScene) : Attribute
                {
                    public int PoolSize { get; set; }
                    public string? ItemCommand { get; set; }
                }
            }

            namespace DotPudica.Godot.Binding.ControlProxies
            {
                public class VirtualizedItemsControl : global::Godot.Node { }
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public DotPudica.Core.Binding.BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindItems<TCollection>(
                        object target, string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath, int poolSize,
                        Func<TViewModel, ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                    public void BindVirtualizedItems<TCollection>(
                        DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                        string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath,
                        Func<TViewModel, ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
                public sealed class SampleItem { }

                public sealed class SampleViewModel
                {
                    public ObservableCollection<SampleItem> Items { get; } = [];

                    [RelayCommand]
                    private void SelectItem(SampleItem item) { }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [ItemsSource("Items", "res://item.tscn", ItemCommand = "SelectItemCommand")]
                    private Container items = null!;
                }
            """;

        // The SelectItemCommand property generated by [RelayCommand] comes from CommunityToolkit.Mvvm's separate source generator,
        // and will not actually exist in a single-generator sandbox compilation (which corresponds to the cross-generator visibility fallback scenario in production).
        // Therefore, we disable the overall compilation check and only verify that this generator produces no error diagnostics
        // and that the generated binding code text matches expectations.
        var (result, generated) = RunGeneratorWithDiagnostics(source, requireCleanCompilation: false);

        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Contains(
            "private static readonly Func<Sample.SampleViewModel, ICommand> __itemcmd_SelectItemCommand_get = static vm => vm.SelectItemCommand;",
            generated);
        Assert.Contains(
            "__dotPudicaView.BindItems(items, \"res://item.tscn\", __itemsPath, 0, __itemcmd_SelectItemCommand_get);",
            generated);
    }

    [Fact]
    public void ItemsSource_ItemCommandParameterMismatch_ReportsDiagnosticAndOmitsItemCommand()
    {
        const string source = """
            global using System;
            using System.Collections.ObjectModel;
            using System.Windows.Input;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Core.Binding.Attributes;
            using DotPudica.Godot.Views;
            using CommunityToolkit.Mvvm.Input;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }

                public class Container : Node { }
            }

            namespace CommunityToolkit.Mvvm.Input
            {
                public sealed class RelayCommandAttribute : Attribute { }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Core.Binding.Attributes
            {
                public sealed class ItemsSourceAttribute(string path, string itemScene) : Attribute
                {
                    public int PoolSize { get; set; }
                    public string? ItemCommand { get; set; }
                }
            }

            namespace DotPudica.Godot.Binding.ControlProxies
            {
                public class VirtualizedItemsControl : global::Godot.Node { }
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public DotPudica.Core.Binding.BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindItems<TCollection>(
                        object target, string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath, int poolSize,
                        Func<TViewModel, ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                    public void BindVirtualizedItems<TCollection>(
                        DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                        string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath,
                        Func<TViewModel, ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
                public sealed class SampleItem { }
                public sealed class OtherItem { }

                public sealed class SampleViewModel
                {
                    public ObservableCollection<SampleItem> Items { get; } = [];

                    [RelayCommand]
                    private void SelectItem(OtherItem item) { }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [ItemsSource("Items", "res://item.tscn", ItemCommand = "SelectItemCommand")]
                    private Container items = null!;
                }
            }
            """;

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA036" && diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain("itemcmd_SelectItemCommand", generated);
        Assert.Contains(
            "__dotPudicaView.BindItems(items, \"res://item.tscn\", __itemsPath, 0);",
            generated);
    }

    private static string TypeCheckHarness(string sampleTypes, bool includeDelegateProxy = false)
    {
        var delegateProxy = includeDelegateProxy
            ? """
                public sealed class DelegateTargetProxy<TControl, TValue>
                {
                    public DelegateTargetProxy(
                        TControl control,
                        Func<TControl, TValue> getter,
                        Action<TControl, TValue>? setter,
                        string? changeSignal) { }
                }
              """
            : "";

        return $$"""
            global using System;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Core.Binding.Attributes;
            using DotPudica.Godot.Views;

            namespace Godot
            {
                public class Node
                {
                    public virtual void _Ready() { }
                    public virtual void _ExitTree() { }
                }

                public class Label : Node
                {
                    public string Text { get; set; } = "";
                }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Binding
            {
                public enum BindingMode { Default, OneWay, TwoWay, OneWayToSource, OneTime }
                public class BindingContext { }

                public sealed class TypedBindingPath<TSource, TValue>
                {
                    public TypedBindingPath(
                        Func<TSource, TValue> getter,
                        Action<TSource, TValue>? setter,
                        string[] segments,
                        Func<TSource, object?>[]? prefixGetters = null) { }
                }
            }

            namespace DotPudica.Core.Binding.Attributes
            {
                public sealed class BindToAttribute(string path) : Attribute
                {
                    public BindingMode Mode { get; set; }
                    public string? Target { get; set; }
                    public string? Signal { get; set; }
                    public Type? Converter { get; set; }
                }
            }

            namespace DotPudica.Godot.Binding.ControlProxies
            {
                public class LabelProxy
                {
                    public LabelProxy(Label label) { }
                }

                public class VirtualizedItemsControl : global::Godot.Node { }

                {{delegateProxy}}
            }

            namespace DotPudica.Godot.Views
            {
                public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Dispose() { }
                    public void BindProperty<TSource, TTarget>(
                        object targetProxy,
                        TypedBindingPath<TViewModel, TSource> sourcePath,
                        BindingMode mode,
                        object? converter = null,
                        Func<TSource, TTarget>? mapForward = null,
                        Func<TTarget, TSource>? mapBack = null) { }
                    public void BindVirtualizedItems<TCollection>(
                        DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                        string scene,
                        TypedBindingPath<TViewModel, TCollection> sourcePath,
                        Func<TViewModel, System.Windows.Input.ICommand>? itemCommandGetter = null)
                        where TCollection : class { }
                }
            }

            namespace Sample
            {
            {{sampleTypes}}
            }
            """;
    }

    private static string NullableProxyHarness(string sampleTypes) => $$"""
        #nullable enable
        global using System;
        using Godot;
        using DotPudica.Core.Binding;
        using DotPudica.Core.Binding.Attributes;
        using DotPudica.Godot.Views;

        namespace Godot
        {
            public struct Color { }

            public class Node
            {
                public virtual void _Ready() { }
                public virtual void _ExitTree() { }
            }

            public class Label : Node
            {
                public string Text { get; set; } = "";
            }

            public class Texture2D { }

            public class TextureRect : Node
            {
                public Texture2D Texture { get; set; } = null!;
                public Color Modulate { get; set; }
            }
        }

        namespace DotPudica.Core.ViewModels
        {
            public enum ViewModelOwnership { External, Owned }
        }

        namespace DotPudica.Core.Binding
        {
            public enum BindingMode { Default, OneWay, TwoWay, OneWayToSource, OneTime }
            public class BindingContext { }

            public interface ITypedTargetProxy<TValue> : IDisposable
            {
                TValue GetValue();
                void SetValue(TValue value);
                event EventHandler? ValueChanged;
            }

            public interface IValueConverter<TIn, TOut>
            {
                TOut Convert(TIn value);
                TIn ConvertBack(TOut value);
            }

            public sealed class TypedBindingPath<TSource, TValue>
            {
                public TypedBindingPath(
                    Func<TSource, TValue> getter,
                    Action<TSource, TValue>? setter,
                    string[] segments,
                    Func<TSource, object?>[]? prefixGetters = null) { }
            }
        }

        namespace DotPudica.Core.Binding.Attributes
        {
            public sealed class BindToAttribute(string path) : Attribute
            {
                public BindingMode Mode { get; set; }
                public string? Target { get; set; }
                public string? Signal { get; set; }
                public Type? Converter { get; set; }
            }
        }

        namespace DotPudica.Godot.Binding.ControlProxies
        {
            public class LabelProxy : ITypedTargetProxy<string>
            {
                public LabelProxy(Label label) { }
                public string GetValue() => "";
                public void SetValue(string value) { }
                public event EventHandler? ValueChanged { add { } remove { } }
                public void Dispose() { }
            }

            public class TextureRectProxy : ITypedTargetProxy<Texture2D?>
            {
                public TextureRectProxy(TextureRect textureRect) { }
                public Texture2D? GetValue() => null;
                public void SetValue(Texture2D? value) { }
                public event EventHandler? ValueChanged { add { } remove { } }
                public void Dispose() { }
            }

            public sealed class DelegateTargetProxy<TControl, TValue> : ITypedTargetProxy<TValue>
            {
                public DelegateTargetProxy(
                    TControl control,
                    Func<TControl, TValue> getter,
                    Action<TControl, TValue>? setter,
                    string? changeSignal) { }
                public TValue GetValue() => default!;
                public void SetValue(TValue value) { }
                public event EventHandler? ValueChanged { add { } remove { } }
                public void Dispose() { }
            }

            public class VirtualizedItemsControl : global::Godot.Node { }
        }

        namespace DotPudica.Godot.Views
        {
            public sealed class DotPudicaViewAttribute(Type viewModelType) : Attribute { }

            public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
            {
                public BindingContext BindingContext { get; } = new();
                public TViewModel? ViewModel { get; set; }
                public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                public void CaptureUiContext() { }
                public void Dispose() { }
                public void BindProperty<TSource, TTarget>(
                    ITypedTargetProxy<TTarget> targetProxy,
                    TypedBindingPath<TViewModel, TSource> sourcePath,
                    BindingMode mode,
                    IValueConverter<TSource, TTarget>? converter = null,
                    Func<TSource, TTarget>? mapForward = null,
                    Func<TTarget, TSource>? mapBack = null) { }
                public void BindVirtualizedItems<TCollection>(
                    DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl target,
                    string scene,
                    TypedBindingPath<TViewModel, TCollection> sourcePath,
                    Func<TViewModel, System.Windows.Input.ICommand>? itemCommandGetter = null)
                    where TCollection : class { }
            }
        }

        namespace Sample
        {
        {{sampleTypes}}
        }
        """;

    private static string RunGenerator(string source)
    {
        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return generated;
    }

    private static (GeneratorRunResult Result, string Generated) RunGeneratorWithDiagnostics(
        string source, bool requireCleanCompilation = true)
    {
        var (result, generated, _) = RunGeneratorWithCompilationDiagnostics(source, requireCleanCompilation);
        return (result, generated);
    }

    private static (GeneratorRunResult Result, string Generated, IReadOnlyList<Diagnostic> CompilationDiagnostics)
        RunGeneratorWithCompilationDiagnostics(
            string source,
            bool requireCleanCompilation = true,
            NullableContextOptions nullableContext = NullableContextOptions.Disable)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "BindingGeneratorRegression",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: GetPlatformReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: nullableContext));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BindingGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var result = Assert.Single(driver.GetRunResult().Results);
        var compilationDiagnostics = outputCompilation.GetDiagnostics();
        if (requireCleanCompilation && !result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            Assert.Empty(compilationDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        return (result, Assert.Single(result.GeneratedSources).SourceText.ToString(), compilationDiagnostics);
    }

    private static IEnumerable<MetadataReference> GetPlatformReferences()
    {
        var trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
