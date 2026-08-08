using DotPudica.Godot.Binding.ControlProxies;

namespace Samples.Showcase.Gallery.VirtualList;

/// <summary>
/// Main project script bridge type: Godot scene/programmatic instantiation can only use C# scripts from the current project assembly;
/// the virtualization implementation itself is still provided by DotPudica.Godot's VirtualizedItemsControl.
/// </summary>
public partial class VirtualListItemsControl : VirtualizedItemsControl
{
}
