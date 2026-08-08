using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.VirtualList;

/// <summary>Immutable virtual-list item.</summary>
public sealed record VirtualListItemModel(int Id, string Title);

/// <summary>
/// Virtual list demo: 10,000 items via VirtualizedItemsControl — only visible rows are instantiated.
/// </summary>
public partial class VirtualListViewModel : ViewModelBase
{
    private int _nextId = 1;

    public ObservableCollection<VirtualListItemModel> Items { get; } = new();

    public int ItemCount => Items.Count;

    [ObservableProperty]
    private string _status = "Loading…";

    [ObservableProperty]
    private string _activeText = "—";

    [ObservableProperty]
    private string _rangeText = "—";

    public VirtualListViewModel()
    {
        for (var i = 0; i < 10_000; i++)
            Items.Add(CreateItem());

        Status = $"Loaded {Items.Count:N0} items — scroll to see node reuse.";
    }

    [RelayCommand]
    private void InsertAtStart()
    {
        Items.Insert(0, CreateItem());
        RefreshCount();
        Status = $"Inserted at start — {Items.Count:N0} items.";
    }

    [RelayCommand]
    private void RemoveMiddle()
    {
        if (Items.Count == 0)
            return;

        Items.RemoveAt(Items.Count / 2);
        RefreshCount();
        Status = $"Removed middle — {Items.Count:N0} items.";
    }

    [RelayCommand]
    private void MoveFirstToLast()
    {
        if (Items.Count < 2)
            return;

        Items.Move(0, Items.Count - 1);
        Status = "Moved first to last.";
    }

    private VirtualListItemModel CreateItem() => new(_nextId, $"Item #{_nextId++:D5}");

    /// <summary>Called by the View on scroll/resize to refresh virtualization metrics.</summary>
    public void UpdateVirtualStats(int active, int firstVisible, int visibleCount)
    {
        ActiveText = $"{active} nodes";
        RangeText = $"rows {firstVisible}–{firstVisible + Math.Max(visibleCount, 1) - 1}";
    }

    private void RefreshCount() => OnPropertyChanged(nameof(ItemCount));
}
