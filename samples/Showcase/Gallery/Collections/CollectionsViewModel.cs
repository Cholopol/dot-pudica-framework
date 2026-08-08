using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Collections;

/// <summary>Immutable list item — no INotifyPropertyChanged required.</summary>
public sealed record CollectionItemModel(int Id, string Title);

/// <summary>
/// Collections binding demo: one ObservableCollection drives PoolSize=0 and PoolSize=32
/// [ItemsSource] containers side by side.
/// </summary>
public partial class CollectionsViewModel : ViewModelBase
{
    private int _nextId = 1;

    public ObservableCollection<CollectionItemModel> Items { get; } = new();

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private string _unpooledStatsText = "Alive=0 · Created=0 · Reused=0";

    [ObservableProperty]
    private string _pooledStatsText = "Alive=0 · Created=0 · Reused=0";

    public CollectionsViewModel()
    {
        for (var i = 0; i < 5; i++)
            Items.Add(CreateItem());

        StatusText = $"Loaded {Items.Count} items.";
        RefreshCommandStates();
    }

    [RelayCommand]
    private void Add()
    {
        Items.Add(CreateItem());
        StatusText = $"Added — {Items.Count} items.";
        RefreshCommandStates();
    }

    private bool CanRemove() => Items.Count > 0;

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void RemoveLast()
    {
        Items.RemoveAt(Items.Count - 1);
        StatusText = $"Removed last — {Items.Count} items.";
        RefreshCommandStates();
    }

    private bool CanMove() => Items.Count > 1;

    [RelayCommand(CanExecute = nameof(CanMove))]
    private void MoveFirstToLast()
    {
        var first = Items[0];
        Items.RemoveAt(0);
        Items.Add(first);
        StatusText = "Moved first to last.";
    }

    [RelayCommand]
    private void Clear()
    {
        Items.Clear();
        StatusText = "Cleared.";
        RefreshCommandStates();
    }

    private CollectionItemModel CreateItem() => new(_nextId, $"Item #{_nextId++:D3}");

    /// <summary>Called by the View after list child enter/exit events to refresh per-column pool metrics.</summary>
    public void UpdatePoolStats(bool pooled, int alive, int created, int reused)
    {
        var text = $"Alive={alive} · Created={created} · Reused={reused}";
        if (pooled)
            PooledStatsText = text;
        else
            UnpooledStatsText = text;
    }

    private void RefreshCommandStates()
    {
        RemoveLastCommand.NotifyCanExecuteChanged();
        MoveFirstToLastCommand.NotifyCanExecuteChanged();
    }
}
