namespace Samples.Showcase.Gallery.Diagnostics;

/// <summary>Diagnostic catalog entry — mirrors addons/dot-pudica/SourceGenerator/DiagnosticDescriptors.cs.</summary>
public sealed record DiagnosticInfo(string Id, string Title, string Severity, string Description);

/// <summary>
/// DotPudica source-generator diagnostic codes (synced from DiagnosticDescriptors.cs).
/// </summary>
public static class DiagnosticCatalog
{
    public static readonly IReadOnlyList<DiagnosticInfo> All = new[]
    {
        new DiagnosticInfo("DOTPUDICA001", "Binding path unresolved", "Error",
            "Cannot resolve [BindTo]/[BindCommand]/[ItemsSource] path — a segment does not exist on the ViewModel."),
        new DiagnosticInfo("DOTPUDICA005", "Command is not ICommand", "Error",
            "[BindCommand] target member does not implement System.Windows.Input.ICommand."),
        new DiagnosticInfo("DOTPUDICA010", "Collection lacks INotifyCollectionChanged", "Error",
            "[ItemsSource] target type does not implement INotifyCollectionChanged."),
        new DiagnosticInfo("DOTPUDICA030", "Path segment is value type", "Error",
            "A middle binding-path segment is a struct; cannot chain INotifyPropertyChanged."),
        new DiagnosticInfo("DOTPUDICA031", "Invalid target property", "Error",
            "Control type has no property matching the [BindTo]/[BindCommand] target."),
        new DiagnosticInfo("DOTPUDICA032", "Type mismatch without converter", "Error",
            "Source and target types are incompatible with no IValueConverter<TIn,TOut> or implicit numeric conversion."),
        new DiagnosticInfo("DOTPUDICA033", "Converter missing typed interface", "Error",
            "Converter type does not implement the required IValueConverter<TIn,TOut>."),
        new DiagnosticInfo("DOTPUDICA034", "Derived→base TwoWay needs converter", "Error",
            "Casting derived to base for TwoWay/OneWayToSource is unsafe without an explicit converter."),
        new DiagnosticInfo("DOTPUDICA035", "Boxing binding forbidden", "Error",
            "Value-type to object/interface binding would box; use same type or an explicit converter."),
        new DiagnosticInfo("DOTPUDICA036", "ItemCommand parameter mismatch", "Error",
            "[ItemsSource] ItemCommand parameter type does not match the collection element type."),
        new DiagnosticInfo("DOTPUDICA040", "ViewModel not DI-resolvable", "Error",
            "ViewModel has no single public constructor with all-interface parameters and no [ViewModelFactory] method."),
        new DiagnosticInfo("DOTPUDICA041", "ViewModelFactory method invalid", "Error",
            "[ViewModelFactory] method must be a parameterless instance method returning the ViewModel type or a derived type."),
        new DiagnosticInfo("DOTPUDICA042", "Subscribe target invalid", "Error",
            "[Subscribe] event cannot be resolved on the ViewModel or the handler signature is incompatible."),
        new DiagnosticInfo("DOTPUDICA043", "Inject target not writable", "Error",
            "[Inject] member must be a writable field or property."),
        new DiagnosticInfo("DOTPUDICA045", "PoolSize on virtualized items", "Error",
            "[ItemsSource] on a virtualized target does not support PoolSize; virtualized controls manage their own recycling."),
        new DiagnosticInfo("DOTPUDICA046", "Lifecycle entry point missing", "Error",
            "Godot lifecycle override must call the generated entry point (e.g. InitializeView/DisposeView) — Godot only dispatches user-declared overrides."),
    };
}
