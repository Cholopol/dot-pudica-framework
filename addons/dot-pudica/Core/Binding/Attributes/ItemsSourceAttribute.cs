namespace DotPudica.Core.Binding.Attributes;

/// <summary>
/// Thread contract: bound ObservableCollections may only be mutated on the UI thread.
/// Background work should produce data, then apply via the UI dispatcher (or
/// <c>LatestSnapshotMailbox</c> to merge snapshots and apply once).
/// For large lists beyond the viewport, prefer <c>VirtualizedItemsControl</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ItemsSourceAttribute : Attribute
{
    public string Path { get; }

    public string ItemScene { get; }

    /// <summary>
    /// Recycled item-view capacity; 0 destroys removed views immediately.
    /// Opt-in: a pooled scene must safely leave and re-enter the scene tree.
    /// </summary>
    public int PoolSize { get; set; }

    /// <summary>
    /// ICommand path on the ViewModel, injected into each item's
    /// <see cref="DotPudica.Godot.Binding.ControlProxies.IItemsControlItemCommand.ItemCommand"/>
    /// with the parameter fixed to the row DataContext—avoids a per-row ViewModel or cross-scene messaging.
    /// If from [RelayCommand], the generator validates the parameter type against the element type.
    /// </summary>
    public string? ItemCommand { get; set; }

    public ItemsSourceAttribute(string path, string itemScene)
    {
        Path = path;
        ItemScene = itemScene;
    }
}
