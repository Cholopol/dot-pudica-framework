using System.Collections;
using System.Collections.Specialized;

namespace DotPudica.Core.Binding;

/// <summary>
/// Visible-window binding—unlike <see cref="CollectionBinding"/>, which creates one node per source item.
/// Source must not mutate concurrently with target refreshes; worker-thread changes need their own sync.
/// Source-path rebindings are marshalled onto the UI dispatcher before the list is read.
/// </summary>
public sealed class VirtualizedCollectionBinding : IDisposable, IBinding
{
    private readonly IBindingPath _sourcePath;
    private readonly IVirtualizedItemsTargetProxy _targetProxy;
    private readonly IUiDispatcher _dispatcher;
    private readonly Action _runSourceSync;
    private readonly Action _runRefresh;
    private INotifyCollectionChanged? _currentCollection;
    private bool _disposed;
    private readonly UiDispatchCoalescer _coalescer = new();
    private readonly UiDispatchCoalescer.Channel _sourceSyncChannel;
    private readonly UiDispatchCoalescer.Channel _refreshChannel;

    public VirtualizedCollectionBinding(
        IVirtualizedItemsTargetProxy targetProxy,
        IBindingPath sourcePath,
        IUiDispatcher? dispatcher = null)
    {
        _targetProxy = targetProxy;
        _sourcePath = sourcePath;
        _dispatcher = dispatcher ?? UiDispatcher.Immediate;
        _sourceSyncChannel = _coalescer.CreateChannel();
        _refreshChannel = _coalescer.CreateChannel();
        _runSourceSync = RunSourceSync;
        _runRefresh = RunRefresh;
        _sourcePath.ValueChanged += OnSourceValueChanged;
    }

    public void Bind(object? source)
    {
        VerifyUiAccess();
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
        _coalescer.AdvanceVersion();
        _sourcePath.Bind(source);
        ScheduleSourceSync();
    }

    public void Unbind()
    {
        VerifyUiAccess();
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
        _coalescer.AdvanceVersion();
        _refreshChannel.Clear();
        DetachCollection();
        _targetProxy.SetItems(null);
        _sourcePath.Unbind();
    }

    private void OnSourceValueChanged(object? sender, EventArgs e)
    {
        _coalescer.AdvanceVersion();
        ScheduleSourceSync();
    }

    private void SyncItems()
    {
        _refreshChannel.Clear();
        DetachCollection();

        var items = _sourcePath.GetValue() as IList;
        if (items is INotifyCollectionChanged collection)
        {
            _currentCollection = collection;
            _currentCollection.CollectionChanged += OnCollectionChanged;
        }

        _targetProxy.SetItems(items);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var version = _coalescer.CurrentVersion;
        if (!TryScheduleRefresh(version))
            return;

        if (_dispatcher.CheckAccess())
        {
            RunRefresh();
            return;
        }

        _dispatcher.Post(_runRefresh);
    }

    private void ScheduleSourceSync()
    {
        _sourceSyncChannel.Stamp(_coalescer.CurrentVersion);
        _sourceSyncChannel.TryMarkQueued(() => _dispatcher.Post(_runSourceSync));
    }

    private void RunSourceSync()
    {
        _sourceSyncChannel.ClearScheduled();
        if (!_disposed && _sourceSyncChannel.ReadVersion() == _coalescer.CurrentVersion)
            SyncItems();
    }

    private void RunRefresh()
    {
        var version = _refreshChannel.ReadVersion();
        _refreshChannel.ClearIf(version);

        if (!_disposed && version == _coalescer.CurrentVersion)
            _targetProxy.Refresh();
    }

    private void DetachCollection()
    {
        if (_currentCollection is null)
            return;

        _currentCollection.CollectionChanged -= OnCollectionChanged;
        _currentCollection = null;
    }

    public void Dispose()
    {
        VerifyUiAccess();
        if (_disposed)
            return;

        _coalescer.AdvanceVersion();
        _refreshChannel.Clear();
        DetachCollection();
        _sourcePath.ValueChanged -= OnSourceValueChanged;
        _sourcePath.Dispose();
        _targetProxy.Dispose();
        _disposed = true;
    }

    private void VerifyUiAccess()
    {
        if (!_dispatcher.CheckAccess())
            throw new InvalidOperationException("Virtualized collection binding lifecycle operations must be executed on the UI thread.");
    }

    private bool TryScheduleRefresh(long version) => _refreshChannel.TryStampIfNew(version);
}
