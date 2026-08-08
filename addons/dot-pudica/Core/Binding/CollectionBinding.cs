using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace DotPudica.Core.Binding;

/// <summary>
/// Thread contract: bound ObservableCollections may only be mutated on the UI thread.
/// Source-path rebinding and Reset snapshots run on the UI dispatcher; the source must
/// still avoid concurrent modification with UI sync.
/// </summary>
public class CollectionBinding : IDisposable, IBinding
{
    private readonly IBindingPath _sourcePath;
    private readonly IItemsTargetProxy _targetProxy;
    private readonly IUiDispatcher _dispatcher;
    private readonly Action _runFullSync;
    private readonly Action _runCollectionChanges;
    private readonly object _pendingChangesLock = new();
    private readonly Queue<PendingCollectionChange> _pendingChanges = new();
    private INotifyCollectionChanged? _currentCollection;
    private bool _disposed;
    private readonly UiDispatchCoalescer _coalescer = new();
    private readonly UiDispatchCoalescer.Channel _fullSyncChannel;
    private bool _collectionChangesScheduled;

    public CollectionBinding(
        IItemsTargetProxy targetProxy,
        IBindingPath sourcePath,
        IUiDispatcher? dispatcher = null)
    {
        _targetProxy = targetProxy;
        _sourcePath = sourcePath;
        _dispatcher = dispatcher ?? UiDispatcher.Immediate;
        _fullSyncChannel = _coalescer.CreateChannel();
        _runFullSync = RunFullSync;
        _runCollectionChanges = RunCollectionChanges;
        _sourcePath.ValueChanged += OnSourceValueChanged;
    }

    public void Bind(object? source)
    {
        VerifyUiAccess();
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
        _coalescer.AdvanceVersion();
        _sourcePath.Bind(source);
        ScheduleFullSync();
    }

    public void Unbind()
    {
        VerifyUiAccess();
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
        _coalescer.AdvanceVersion();
        ClearPendingChanges();
        DetachCollection();
        _targetProxy.Clear();
        _sourcePath.Unbind();
    }

    private void OnSourceValueChanged(object? sender, EventArgs e)
    {
        _coalescer.AdvanceVersion();
        ScheduleFullSync();
    }

    private void SyncFullCollection()
    {
        DetachCollection();

        var current = _sourcePath.GetValue();
        if (current is not INotifyCollectionChanged notifyCollection)
        {
            _targetProxy.Clear();
            return;
        }

        _currentCollection = notifyCollection;
        _currentCollection.CollectionChanged += OnCollectionChanged;

        var items = current is IList list ? Snapshot(list) : [];
        _targetProxy.Clear();
        for (int i = 0; i < items.Count; i++)
            _targetProxy.Add(items[i], i);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var change = new PendingCollectionChange(_coalescer.CurrentVersion, sender, e);
        if (_dispatcher.CheckAccess())
        {
            ApplyCollectionChange(change);
            return;
        }

        var shouldPost = false;
        lock (_pendingChangesLock)
        {
            if (_disposed)
                return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
                _pendingChanges.Clear();

            _pendingChanges.Enqueue(change);
            if (!_collectionChangesScheduled)
            {
                _collectionChangesScheduled = true;
                shouldPost = true;
            }
        }

        if (shouldPost)
            _dispatcher.Post(_runCollectionChanges);
    }

    private void ApplyCollectionChange(PendingCollectionChange change)
    {
        if (_disposed || change.Version != _coalescer.CurrentVersion)
            return;

        var e = change.EventArgs;
        var resetItems = e.Action == NotifyCollectionChangedAction.Reset && change.Sender is IList resetList
            ? Snapshot(resetList)
            : null;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    var startIndex = e.NewStartingIndex < 0 ? 0 : e.NewStartingIndex;
                    for (int i = 0; i < e.NewItems.Count; i++)
                        _targetProxy.Add(e.NewItems[i], startIndex + i);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    // Reverse order keeps indices stable while removing a contiguous range.
                    var startIndex = e.OldStartingIndex < 0 ? 0 : e.OldStartingIndex;
                    for (int i = e.OldItems.Count - 1; i >= 0; i--)
                        _targetProxy.RemoveAt(startIndex + i);
                }
                break;

            case NotifyCollectionChangedAction.Move:
                if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0)
                    _targetProxy.Move(e.OldStartingIndex, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems is not null && e.NewStartingIndex >= 0)
                {
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        var index = e.NewStartingIndex + i;
                        _targetProxy.RemoveAt(index);
                        _targetProxy.Add(e.NewItems[i], index);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                _targetProxy.Clear();
                if (resetItems is not null)
                {
                    for (int i = 0; i < resetItems.Count; i++)
                        _targetProxy.Add(resetItems[i], i);
                }
                break;
        }
    }

    private void ScheduleFullSync()
    {
        _fullSyncChannel.Stamp(_coalescer.CurrentVersion);
        _fullSyncChannel.TryMarkQueued(() => _dispatcher.Post(_runFullSync));
    }

    private void RunFullSync()
    {
        _fullSyncChannel.ClearScheduled();
        if (!_disposed && _fullSyncChannel.ReadVersion() == _coalescer.CurrentVersion)
            SyncFullCollection();
    }

    private void RunCollectionChanges()
    {
        while (true)
        {
            PendingCollectionChange change;
            lock (_pendingChangesLock)
            {
                if (_pendingChanges.Count == 0)
                {
                    _collectionChangesScheduled = false;
                    return;
                }

                change = _pendingChanges.Dequeue();
            }

            ApplyCollectionChange(change);
        }
    }

    private void ClearPendingChanges()
    {
        lock (_pendingChangesLock)
            _pendingChanges.Clear();
    }

    private static List<object?> Snapshot(IList list)
    {
        var items = new List<object?>(list.Count);
        for (int i = 0; i < list.Count; i++)
            items.Add(list[i]);
        return items;
    }

    private void DetachCollection()
    {
        if (_currentCollection is not null)
        {
            _currentCollection.CollectionChanged -= OnCollectionChanged;
            _currentCollection = null;
        }
    }

    public void Dispose()
    {
        VerifyUiAccess();
        if (_disposed)
            return;

        _coalescer.AdvanceVersion();
        ClearPendingChanges();
        DetachCollection();
        _sourcePath.ValueChanged -= OnSourceValueChanged;
        _sourcePath.Dispose();
        _disposed = true;
        _targetProxy.Dispose();
    }

    private void VerifyUiAccess()
    {
        if (!_dispatcher.CheckAccess())
            throw new InvalidOperationException("Collection binding lifecycle operations must be executed on the UI thread.");
    }

    private readonly record struct PendingCollectionChange(
        long Version,
        object? Sender,
        NotifyCollectionChangedEventArgs EventArgs);
}
