namespace DotPudica.Core.Binding.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BindToAttribute : Attribute
{
    public string Path { get; }

    /// <summary>Default is inferred from the control type by the source generator.</summary>
    public BindingMode Mode { get; set; } = BindingMode.Default;

    /// <summary>Must implement IValueConverter (or a typed variant).</summary>
    public Type? Converter { get; set; }

    /// <summary>Override target property name when inference fails.</summary>
    public string? Target { get; set; }

    /// <summary>Override change signal name when inference fails.</summary>
    public string? Signal { get; set; }

    public BindToAttribute(string path)
    {
        Path = path;
    }
}
