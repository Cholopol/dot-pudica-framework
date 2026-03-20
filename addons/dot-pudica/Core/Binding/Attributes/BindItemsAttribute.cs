namespace DotPudica.Core.Binding.Attributes;

/// <summary>
/// Declarative collection binding. Mark on View's list container controls to automatically bind to ViewModel's ObservableCollection.
/// </summary>
/// <example>
/// <code>
/// [Export, BindItems("Items", TemplatePath = "res://scenes/item_template.tscn")]
/// VBoxContainer itemList;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class BindItemsAttribute : Attribute
{
    /// <summary>
    /// The collection property path on the ViewModel.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// List item template scene path (Godot PackedScene path).
    /// </summary>
    public string? TemplatePath { get; set; }

    public BindItemsAttribute(string path)
    {
        Path = path;
    }
}
