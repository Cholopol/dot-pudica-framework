namespace DotPudica.Core.Binding.Attributes;

/// <summary>
/// Declarative property binding. Mark on fields in a partial view class to automatically establish bindings to ViewModel properties.
/// The DotPudica source generator scans these attributes and injects the binding setup code into the view stub.
/// </summary>
/// <example>
/// <code>
/// [Export, BindTo("Username")]
/// LineEdit usernameInput;
///
/// [Export, BindTo("ErrorMessage", Mode = BindingMode.OneWay)]
/// Label errorLabel;
///
/// [Export, BindTo("Score", Mode = BindingMode.OneWay, Converter = typeof(IntToStringConverter))]
/// Label scoreLabel;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class BindToAttribute : Attribute
{
    /// <summary>
    /// The property path on the ViewModel, supports nested paths (e.g., "Account.Username").
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Binding mode. Default is Default (automatically inferred from control type).
    /// </summary>
    public BindingMode Mode { get; set; } = BindingMode.Default;

    /// <summary>
    /// Value converter type, must implement <see cref="IValueConverter"/>.
    /// </summary>
    public Type? Converter { get; set; }

    /// <summary>
    /// Target control property name. Default is automatically inferred by Source Generator based on control type.
    /// </summary>
    public string? TargetProperty { get; set; }

    /// <summary>
    /// Control change event name for TwoWay binding. Default is automatically inferred by Source Generator based on control type.
    /// </summary>
    public string? SourceEvent { get; set; }

    public BindToAttribute(string path)
    {
        Path = path;
    }
}
