namespace DotPudica.Core.Binding;

/// <summary>
/// Abstract proxy interface for target control properties.
/// Concrete implementations are provided by the Godot adapter layer for various controls (Label, LineEdit, Button, etc.).
/// </summary>
public interface ITargetProxy : IDisposable
{
    /// <summary>
    /// Get the current property value of the control.
    /// </summary>
    object? GetValue();

    /// <summary>
    /// Set the control property value.
    /// </summary>
    void SetValue(object? value);

    /// <summary>
    /// Triggered when the control value is modified by user (used for TwoWay binding).
    /// </summary>
    event EventHandler? ValueChanged;

    /// <summary>
    /// Target property type.
    /// </summary>
    Type TargetType { get; }
}
