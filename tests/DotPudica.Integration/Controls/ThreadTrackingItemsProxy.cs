using System.Collections.Generic;
using DotPudica.Core.Binding;

namespace DotPudica.Integration.Controls;

/// <summary>Test stub that records the thread ID of collection proxy reads/writes, used to verify Reset/sync does not reach the target on a background thread.</summary>
public sealed class ThreadTrackingItemsProxy : IItemsTargetProxy
{
    private readonly List<object?> _items = new();
    private bool _disposed;

    public List<int> MutationThreadIds { get; } = new();
    public IReadOnlyList<object?> Items => _items;

    public void Add(object? item, int index)
    {
        Record();
        _items.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        Record();
        if (index >= 0 && index < _items.Count)
            _items.RemoveAt(index);
    }

    public void Move(int oldIndex, int newIndex)
    {
        Record();
        var item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);
    }

    public void Clear()
    {
        Record();
        _items.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _items.Clear();
        _disposed = true;
    }

    private void Record() => MutationThreadIds.Add(System.Environment.CurrentManagedThreadId);
}
