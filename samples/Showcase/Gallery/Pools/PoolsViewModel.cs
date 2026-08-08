using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Interactivity;
using DotPudica.Core.ObjectPool;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Pools;

/// <summary>
/// Pools Gallery ViewModel. CLR <see cref="ObjectPool{T}"/> allocation lives here;
/// NodePool actions are raised via <see cref="InteractionRequest"/> and handled in the View.
/// </summary>
public partial class PoolsViewModel : ViewModelBase
{
    private readonly PoolItemFactory _factory = new();
    private readonly ObjectPool<PoolItem> _corePool;
    private readonly Stack<PoolItem> _allocatedCoreItems = new();

    public PoolsViewModel()
    {
        _corePool = new ObjectPool<PoolItem>(_factory, initialSize: 0, maxSize: 8);
        RefreshCoreStats();
    }

    [ObservableProperty]
    private string _coreStatsText = "";

    [ObservableProperty]
    private string _nodeStatsText = "Displayed=0 · direct new created=0, destroyed=0";

    [ObservableProperty]
    private bool _useNodePool = true;

    [ObservableProperty]
    private string _pooledStatsText = "Live=0 · created=0, reused=0, destroyed=0";

    [ObservableProperty]
    private string _autoPooledStatsText = "Auto: live=0 · created=0, reused=0, destroyed=0";

    public InteractionRequest AllocateNodeRequest { get; } = new();
    public InteractionRequest FreeNodeRequest { get; } = new();
    public InteractionRequest AllocatePooledRequest { get; } = new();
    public InteractionRequest FreePooledRequest { get; } = new();
    public InteractionRequest AllocateAutoPooledRequest { get; } = new();
    public InteractionRequest FreeAutoPooledRequest { get; } = new();

    [RelayCommand]
    private void AllocateCore()
    {
        _allocatedCoreItems.Push(_corePool.Allocate());
        RefreshCoreStats();
    }

    private bool CanFreeCore() => _allocatedCoreItems.Count > 0;

    [RelayCommand(CanExecute = nameof(CanFreeCore))]
    private void FreeCore()
    {
        if (_allocatedCoreItems.TryPop(out var item))
            _corePool.Free(item);
        RefreshCoreStats();
    }

    [RelayCommand]
    private void AllocateNode() => AllocateNodeRequest.Raise();

    [RelayCommand]
    private void FreeNode() => FreeNodeRequest.Raise();

    [RelayCommand]
    private void AllocatePooled() => AllocatePooledRequest.Raise();

    [RelayCommand]
    private void FreePooled() => FreePooledRequest.Raise();

    [RelayCommand]
    private void AllocateAutoPooled() => AllocateAutoPooledRequest.Raise();

    [RelayCommand]
    private void FreeAutoPooled() => FreeAutoPooledRequest.Raise();

    private void RefreshCoreStats()
    {
        CoreStatsText =
            $"Live={_allocatedCoreItems.Count} · Created={_factory.CreatedCount} · " +
            $"Reset={_factory.ResetCalledCount} · Destroyed={_factory.DestroyedCount}";
        FreeCoreCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Called by the View after node allocate/free to refresh stats.</summary>
    public void UpdateNodeStats(int displayedCount, int directCreateCount, int directDestroyCount)
        => NodeStatsText =
            $"Displayed={displayedCount} · direct new created={directCreateCount}, destroyed={directDestroyCount}" +
            (UseNodePool
                ? " · NodePool reuses nodes on repeat Allocate/Free."
                : " · Each Allocate creates a new Label.");

    /// <summary>Called by the View after pooled panel allocate/free to refresh stats.</summary>
    public void UpdatePooledStats(int liveCount, int createCount, int reuseCount, int destroyedCount)
        => PooledStatsText = $"Live={liveCount} · created={createCount}, reused={reuseCount}, destroyed={destroyedCount}";

    /// <summary>Called by the View after auto-init pooled panel allocate/free to refresh stats.</summary>
    public void UpdateAutoPooledStats(int liveCount, int createCount, int reuseCount, int destroyedCount)
        => AutoPooledStatsText =
            $"Auto: live={liveCount} · created={createCount}, reused={reuseCount}, destroyed={destroyedCount}";
}
