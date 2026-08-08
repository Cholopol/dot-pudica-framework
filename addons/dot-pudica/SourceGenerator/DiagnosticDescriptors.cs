using Microsoft.CodeAnalysis;

namespace DotPudica.SourceGenerator;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor PathNotFound = new(
        id: "DOTPUDICA001",
        title: "Binding path cannot be resolved",
        messageFormat: "Cannot resolve property path '{1}' on type '{0}': member '{2}' does not exist",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CommandNotICommand = new(
        id: "DOTPUDICA005",
        title: "Command property is not ICommand",
        messageFormat: "Member '{1}' resolved from path '{0}' has type '{2}', which does not implement System.Windows.Input.ICommand",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CollectionNotObservable = new(
        id: "DOTPUDICA010",
        title: "Collection source does not implement INotifyCollectionChanged",
        messageFormat: "Member '{1}' resolved from path '{0}' has type '{2}', which does not implement System.Collections.Specialized.INotifyCollectionChanged and cannot be used for ItemsSource binding",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StructIntermediatePath = new(
        id: "DOTPUDICA030",
        title: "Intermediate path segment cannot be a value type",
        messageFormat: "Intermediate segment '{1}' of path '{0}' has value type '{2}', which cannot establish INotifyPropertyChanged chained listening",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TargetPropertyInvalid = new(
        id: "DOTPUDICA031",
        title: "Target property is invalid",
        messageFormat: "No usable target property '{1}' found on control type '{0}'",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMismatchWithoutConverter = new(
        id: "DOTPUDICA032",
        title: "Source/target type mismatch without typed converter",
        messageFormat: "Source type '{1}' of binding path '{0}' is incompatible with target type '{2}'; provide a Converter implementing IValueConverter<{1},{2}>, or use implicitly numeric convertible types",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterNotTyped = new(
        id: "DOTPUDICA033",
        title: "Converter does not implement the required typed interface",
        messageFormat: "Converter '{0}' does not implement IValueConverter<{1},{2}> and cannot be used for zero-allocation binding",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TwoWayReferenceUpcastRequiresConverter = new(
        id: "DOTPUDICA034",
        title: "Derived-to-base two-way binding requires a converter",
        messageFormat: "Binding path '{0}' is a reference upcast ('{1}' → '{2}'); two-way/one-way-to-source binding cannot safely write back to the source type; provide an IValueConverter<{1},{2}>, or change to OneWay",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BoxingConversionNotAllowed = new(
        id: "DOTPUDICA035",
        title: "Silent boxing binding is not allowed",
        messageFormat: "Binding path '{0}' from source type '{1}' to target type '{2}' would cause boxing, breaking the zero-allocation hot path; use same-type binding, or provide an explicit IValueConverter<{1},{2}>",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ItemCommandParameterMismatch = new(
        id: "DOTPUDICA036",
        title: "ItemCommand parameter type does not match collection element type",
        messageFormat: "ItemCommand '{0}' of [ItemsSource] has parameter type '{1}', which does not match element type '{3}' of collection '{2}'; " +
                       "the item template will invoke this command with the element as the parameter, please fix the command method parameter type",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ViewModelNotDiResolvable = new(
        id: "DOTPUDICA040",
        title: "ViewModel constructor cannot be resolved from DI",
        messageFormat: "ViewModel '{0}' cannot be constructed by the generated factory: it must have exactly one public constructor whose parameters are all interface types, " +
                       "or the view must declare a [ViewModelFactory] method",
        category: "DotPudicaLifecycle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ViewModelFactoryInvalid = new(
        id: "DOTPUDICA041",
        title: "ViewModelFactory method is invalid",
        messageFormat: "The [ViewModelFactory] method '{0}' must be a parameterless instance method returning '{1}' or a derived type",
        category: "DotPudicaLifecycle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SubscribeInvalid = new(
        id: "DOTPUDICA042",
        title: "Subscribe target is invalid",
        messageFormat: "[Subscribe] event '{0}' could not be resolved on ViewModel '{1}', or handler '{2}' has an incompatible signature",
        category: "DotPudicaLifecycle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectNotWritable = new(
        id: "DOTPUDICA043",
        title: "Inject target is not writable",
        messageFormat: "The [Inject] member '{0}' must be a writable field or property",
        category: "DotPudicaLifecycle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VirtualizedItemsPoolSize = new(
        id: "DOTPUDICA045",
        title: "PoolSize is not applicable to virtualized items",
        messageFormat: "[ItemsSource] on virtualized target '{0}' does not support PoolSize; virtualized controls manage their own recycling",
        category: "DotPudicaBinding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LifecycleEntryPointMissing = new(
        id: "DOTPUDICA046",
        title: "Godot lifecycle override must call the generated entry point",
        messageFormat: "View '{0}' must override '{1}()' and call '{2}()' — Godot only dispatches virtual overrides declared in user source, " +
                       "so the generated lifecycle must be wired from user-written Godot hooks",
        category: "DotPudicaLifecycle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}