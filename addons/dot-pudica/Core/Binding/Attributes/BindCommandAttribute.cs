namespace DotPudica.Core.Binding.Attributes;

/// <summary>
/// Declarative command binding. Mark on View's Button and other control fields to automatically bind to ViewModel's ICommand.
/// </summary>
/// <example>
/// <code>
/// [Export, BindCommand("LoginCommand")]
/// Button loginBtn;
///
/// [Export, BindCommand("DeleteCommand", ParameterPath = "SelectedItem.Id")]
/// Button deleteBtn;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class BindCommandAttribute : Attribute
{
    /// <summary>
    /// The ICommand property name on the ViewModel.
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    /// Optional command parameter path pointing to a property on the ViewModel.
    /// </summary>
    public string? ParameterPath { get; set; }

    public BindCommandAttribute(string commandName)
    {
        CommandName = commandName;
    }
}
