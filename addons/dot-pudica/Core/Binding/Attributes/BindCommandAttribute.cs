namespace DotPudica.Core.Binding.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BindCommandAttribute : Attribute
{
    public string CommandName { get; }

    /// <summary>Optional ViewModel property path used as the command parameter.</summary>
    public string? Parameter { get; set; }

    /// <summary>Override trigger signal when inference fails.</summary>
    public string? Signal { get; set; }

    public BindCommandAttribute(string commandName)
    {
        CommandName = commandName;
    }
}
