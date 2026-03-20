namespace DotPudica.Core.Binding;

/// <summary>
/// Data binding mode, defines the synchronization direction of data between source (ViewModel) and target (View control).
/// </summary>
public enum BindingMode
{
    /// <summary>
    /// Default mode, determined by control type (input controls default to TwoWay, display controls default to OneWay).
    /// </summary>
    Default = 0,

    /// <summary>
    /// One-way binding: ViewModel → View.
    /// </summary>
    OneWay,

    /// <summary>
    /// Two-way binding: ViewModel ↔ View.
    /// </summary>
    TwoWay,

    /// <summary>
    /// One-time binding: only synchronizes once during initial binding.
    /// </summary>
    OneTime,

    /// <summary>
    /// Reverse one-way binding: View → ViewModel.
    /// </summary>
    OneWayToSource
}
