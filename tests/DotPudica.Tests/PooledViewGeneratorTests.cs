using DotPudica.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotPudica.Tests;

/// <summary>
/// Unit tests for poolable views ([DotPudicaView(Pooled = true)]):
/// RecycleView generation and the DOTPUDICA046 diagnostic.
/// </summary>
public class PooledViewGeneratorTests
{
    [Fact]
    public void Pooled_WithAutoInitializeTrue_EmitsRecycleView()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel), Pooled = true)]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => RecycleView();
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains("protected void RecycleView()", generated);
        Assert.Contains("__dotPudicaView.Recycle();", generated);
        Assert.Contains("CreateViewModel()", generated);
        Assert.Contains("OnViewModelBound();", generated);
        Assert.Contains("DotPudicaDispose();", generated);
        Assert.DoesNotContain("DOTPUDICA047", generated);
    }

    [Fact]
    public void Pooled_WithAutoInitializeTrue_ExitTreeDispose_ReportsDiagnostic()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel), Pooled = true)]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => DisposeView();
                }
            """);

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA046"
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Pooled_WithAutoInitializeTrue_NonDiViewModel_ReportsDiagnostic()
    {
        var source = Harness("""
                public sealed class SampleViewModel
                {
                    public SampleViewModel(int score) { }
                }

                [DotPudicaView(typeof(SampleViewModel), Pooled = true)]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => RecycleView();
                }
            """);

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA040"
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void PooledView_EmitsActivateAndRecycleEntries()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel), AutoInitialize = false, Pooled = true)]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => RecycleView();

                    public void BindShared(SampleViewModel shared) => ActivateViewModel(shared);
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains("protected void ActivateViewModel(Sample.SampleViewModel viewModel)", generated);
        Assert.Contains("SetViewModel(viewModel, DotPudica.Core.ViewModels.ViewModelOwnership.External);", generated);
        Assert.Contains("DotPudicaInitialize();", generated);
        Assert.Contains("protected void RecycleView()", generated);
        Assert.Contains("__dotPudicaView.Recycle();", generated);
        Assert.Contains("protected void DisposeView()", generated);
        Assert.Contains("DotPudicaDispose();", generated);
        Assert.DoesNotContain("CreateViewModel", generated);
    }

    [Fact]
    public void PooledView_MissingExitTreeRecycle_ReportsDiagnostic()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel), AutoInitialize = false, Pooled = true)]
                public partial class SampleView : Node
                {
                    public override void _ExitTree() => DisposeView();
                }
            """);

        var (result, _) = RunGeneratorWithDiagnostics(source);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "DOTPUDICA046"
            && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void PooledView_Subscribe_EmitsSubscribeAndUnsubscribeBlocks()
    {
        var source = Harness("""
                public sealed class SampleViewModel
                {
                    public event System.Action? Ping;
                }

                [DotPudicaView(typeof(SampleViewModel), AutoInitialize = false, Pooled = true)]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => RecycleView();

                    [Subscribe("Ping")]
                    private void OnPing() { }
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains("if (ViewModel is { } __vm)", generated);
        Assert.Contains("__vm.Ping += OnPing;", generated);
        Assert.Contains("__vm.Ping -= OnPing;", generated);
    }

    [Fact]
    public void Pooled_WithAutoInitializeTrue_EmitsRequestReady()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel), Pooled = true)]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => RecycleView();
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains("RequestReady();", generated);
    }

    [Fact]
    public void Pooled_WithAutoInitializeFalse_EmitsRequestReady()
    {
        var source = Harness("""
                public sealed class SampleViewModel { }

                [DotPudicaView(typeof(SampleViewModel), AutoInitialize = false, Pooled = true)]
                public partial class SampleView : Node
                {
                    public override void _Ready() => InitializeView();

                    public override void _ExitTree() => RecycleView();

                    public void BindShared(SampleViewModel shared) => ActivateViewModel(shared);
                }
            """);

        var generated = RunGenerator(source);

        Assert.Contains("protected void RecycleView()", generated);
        Assert.Contains("RequestReady();", generated);
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
                    public void RequestReady() { }
                }
            }

            namespace DotPudica.Core.ViewModels
            {
                public enum ViewModelOwnership { External, Owned }
            }

            namespace DotPudica.Core.Composition
            {
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
                    public bool Pooled { get; set; }
                }

                public sealed class DotPudicaViewRuntime<TViewModel> where TViewModel : class
                {
                    public DotPudica.Core.Binding.BindingContext BindingContext { get; } = new();
                    public TViewModel? ViewModel { get; set; }
                    public void SetViewModel(TViewModel viewModel, DotPudica.Core.ViewModels.ViewModelOwnership ownership) { }
                    public void CaptureUiContext() { }
                    public void Recycle() { }
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
            assemblyName: "PooledViewGeneratorRegression",
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
