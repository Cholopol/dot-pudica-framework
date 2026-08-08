using DotPudica.Core.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DotPudica.SourceGenerator;

namespace DotPudica.Tests;

/// <summary>
/// Unit tests for the declarative lifecycle generation: compiled ViewModel factory, [Inject],
/// [Subscribe], virtualized [ItemsSource] and the DOTPUDICA040-046 diagnostics.
/// </summary>
public class DeclarativeLifecycleGeneratorTests
{
    [Fact]
    public void DiResolvableConstructor_GeneratesCompiledFactoryAndLifecycle()
    {
        var source = Harness("""
                public interface IAlphaService { }
                public interface IBetaService { }

                public sealed class SampleViewModel
                {
                    public SampleViewModel(IAlphaService alpha, IBetaService beta) { }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains(
            "private Sample.SampleViewModel CreateViewModel()",
            generated);
        Assert.Contains(
            "new Sample.SampleViewModel(__DotPudicaResolveService<Sample.IAlphaService>(), __DotPudicaResolveService<Sample.IBetaService>());",
            generated);
        Assert.Contains(
            "private static T __DotPudicaResolveService<T>() where T : notnull",
            generated);
        Assert.Contains(
            "return DotPudica.Godot.AppContext.Current.Services.GetRequiredService<T>();",
            generated);
        Assert.Contains("OnViewReady();", generated);
        Assert.Contains(
            "SetViewModel(CreateViewModel(), DotPudica.Core.ViewModels.ViewModelOwnership.Owned);",
            generated);
        Assert.Contains("OnViewModelBound();", generated);
        Assert.Contains("OnViewDisposing();", generated);
        Assert.Contains("DotPudicaDispose();", generated);
    }

    [Fact]
    public void NonDiConstructor_ReportsViewModelNotDiResolvable()
    {
        var source = Harness("""
                public sealed class SampleViewModel
                {
                    public SampleViewModel(int score) { }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();
                }
            """);

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA040"
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ViewModelFactoryMethod_IsInvokedAndNoDiagnostic()
    {
        var source = Harness("""
                public sealed class SampleViewModel
                {
                    public SampleViewModel(int score) { }
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [ViewModelFactory]
                    private SampleViewModel CreateThing() => new(42);
                }
            """);

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.DoesNotContain(result.Diagnostics, d =>
            d.Id == "DOTPUDICA040" && d.Severity == DiagnosticSeverity.Error);
        Assert.Contains("private Sample.SampleViewModel CreateViewModel() => CreateThing();", generated);
    }

    [Fact]
    public void ViewModelFactory_InvalidSignature_ReportsDiagnostic()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [ViewModelFactory]
                    private string CreateThing() => "not a view model";
                }
            """);

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA041"
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Inject_GeneratesAssignmentBeforeOnViewReady()
    {
        var source = Harness("""
                public interface IProfileService { }

                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [Inject]
                    private IProfileService _profile = null!;
                }
            """);

        var generated = RunGenerator(source);

        var injectIndex = generated.IndexOf("_profile = __DotPudicaResolveService<Sample.IProfileService>();", StringComparison.Ordinal);
        var readyIndex = generated.IndexOf("        OnViewReady();", StringComparison.Ordinal);
        Assert.True(injectIndex >= 0, "inject assignment should be generated");
        Assert.True(injectIndex < readyIndex, "inject must run before OnViewReady");
    }

    [Fact]
    public void Inject_ReadonlyField_ReportsDiagnostic()
    {
        var source = Harness("""
                public interface IProfileService { }

                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [Inject]
                    private readonly IProfileService _profile = null!;
                }
            """);

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA043"
            && diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain("_profile = __DotPudicaResolveService", generated);
    }

    [Fact]
    public void Subscribe_GeneratesSubscribeAndUnsubscribeBlocks()
    {
        var source = Harness("""
                public sealed class SampleViewModel
                {
                    public event System.Action? Ping;
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [Subscribe("Ping")]
                    private void OnPing() { }
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains("__vm.Ping += OnPing;", generated);
        Assert.Contains("__vm.Ping -= OnPing;", generated);
        Assert.Contains("if (ViewModel is { } __vm)", generated);
    }

    [Fact]
    public void Subscribe_NestedEventPath_GeneratesMemberAccess()
    {
        var source = Harness("""
                public sealed class NestedRequest
                {
                    public event System.EventHandler? Raised;
                }

                public sealed class SampleViewModel
                {
                    public NestedRequest CreateRequest { get; } = new();
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [Subscribe("CreateRequest.Raised")]
                    private void OnCreateRequested(object? sender, System.EventArgs e) { }
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains("__vm.CreateRequest.Raised += OnCreateRequested;", generated);
        Assert.Contains("__vm.CreateRequest.Raised -= OnCreateRequested;", generated);
    }

    [Fact]
    public void Subscribe_EventNotFound_ReportsDiagnostic()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [Subscribe("NoSuchEvent")]
                    private void OnNothing() { }
                }
            """);

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA042"
            && diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain("OnNothing", generated);
    }

    [Fact]
    public void Subscribe_SignatureMismatch_ReportsDiagnostic()
    {
        var source = Harness("""
                public sealed class SampleViewModel
                {
                    public event System.Action? Ping;
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [Subscribe("Ping")]
                    private void OnPing(int unexpected) { }
                }
            """);

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA042"
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void VirtualizedItemsSource_GeneratesBindVirtualizedItems()
    {
        var source = Harness("""
                public sealed class SampleViewModel
                {
                    public System.Collections.ObjectModel.ObservableCollection<string> Items { get; } = [];
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();

                    [ItemsSource("Items", "res://item.tscn")]
                    private DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl list = null!;
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains("__dotPudicaView.BindVirtualizedItems(list, \"res://item.tscn\", __itemsPath);", generated);
        Assert.DoesNotContain("BindItems(list", generated);
    }

    [Fact]
    public void VirtualizedItemsSource_WithPoolSize_ReportsDiagnostic()
    {
        var source = Harness("""
                public sealed class SampleViewModel
                {
                    public System.Collections.ObjectModel.ObservableCollection<string> Items { get; } = [];
                }

                [DotPudicaView(typeof(SampleViewModel))]
                public partial class SampleView : Node
                {
                    [ItemsSource("Items", "res://item.tscn", PoolSize = 8)]
                    private DotPudica.Godot.Binding.ControlProxies.VirtualizedItemsControl list = null!;
                }
            """);

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA045"
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void AutoInitializeFalse_EmitsTeardownOnly()
    {
        var source = Harness("""
                public interface IInventoryService { }

                public sealed class SampleViewModel
                {
                    public SampleViewModel(IInventoryService inventory) { }
                }

                [DotPudicaView(typeof(SampleViewModel), AutoInitialize = false)]
                public partial class SampleView : Node
                {
                    public override void _ExitTree() => DisposeView();

                    public void BindShared(SampleViewModel shared)
                    {
                        SetViewModel(shared, DotPudica.Core.ViewModels.ViewModelOwnership.External);
                        DotPudicaInitialize();
                    }
                }
            """);

        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.DoesNotContain(result.Diagnostics, d =>
            d.Id is "DOTPUDICA040" or "DOTPUDICA046");
        Assert.DoesNotContain("CreateViewModel", generated);
        Assert.DoesNotContain("public override void _Ready()", generated);
        Assert.Contains("protected void DisposeView()", generated);
        Assert.Contains("partial void OnViewDisposing();", generated);
        Assert.Contains("DotPudicaDispose();", generated);
    }

    [Fact]
    public void OwnershipExternal_IsPassedToSetViewModel()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel), Ownership = DotPudica.Core.ViewModels.ViewModelOwnership.External)]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains(
            "SetViewModel(CreateViewModel(), DotPudica.Core.ViewModels.ViewModelOwnership.External);",
            generated);
    }

    private static string Harness(string sampleTypes)
    {
        return $$"""
            global using System;
            using Godot;
            using DotPudica.Core.Binding;
            using DotPudica.Core.Binding.Attributes;
            using DotPudica.Core.Composition;
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

            namespace DotPudica.Core.Composition
            {
                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
                public sealed class InjectAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Method)]
                public sealed class ViewModelFactoryAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                public sealed class SubscribeAttribute(string eventPath) : Attribute
                {
                    public string EventPath { get; } = eventPath;
                }
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
                public sealed class ItemsSourceAttribute(string path, string itemScene) : Attribute
                {
                    public int PoolSize { get; set; }
                    public string? ItemCommand { get; set; }
                }
            }

            namespace DotPudica.Godot
            {
                public static class AppContext
                {
                    public static AppServices Current => null!;
                }

                public sealed class AppServices
                {
                    public System.IServiceProvider Services => null!;
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
                    public DotPudica.Core.ViewModels.ViewModelOwnership Ownership { get; set; }
                    public bool AutoInitialize { get; set; } = true;
                }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public DotPudica.Core.Binding.BindingContext BindingContext { get; } = new();
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
            {{sampleTypes}}
            }
            """;
    }

    private static string RunGenerator(string source)
    {
        var (result, generated) = RunGeneratorWithDiagnostics(source);

        Assert.Empty(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return generated;
    }

    private static (GeneratorRunResult Result, string Generated) RunGeneratorWithDiagnostics(
        string source, bool requireCleanCompilation = true)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "DeclarativeLifecycleGeneratorRegression",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: GetPlatformReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BindingGenerator().AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var result = Assert.Single(driver.GetRunResult().Results);
        if (requireCleanCompilation && !result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            Assert.Empty(outputCompilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        return (result, Assert.Single(result.GeneratedSources).SourceText.ToString());
    }

    private static IEnumerable<MetadataReference> GetPlatformReferences()
    {
        var trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
