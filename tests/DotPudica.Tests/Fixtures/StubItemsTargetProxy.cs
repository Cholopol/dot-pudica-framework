using System.Collections;
using System.Collections.Generic;
using DotPudica.Core.Binding;

namespace DotPudica.Tests.Fixtures;

/// <summary>
/// IItemsTargetProxy test stub. Records all Add/RemoveAt/Move/Clear calls,
/// and maintains a mirror list to verify the final state.
/// </summary>
public sealed class StubItemsTargetProxy : IItemsTargetProxy
{
    private readonly List<object?> _items = new();
    private bool _disposed;

    /// <summary>Current mirror list of child items (in index order).</summary>
    public IReadOnlyList<object?> Items => _items;

    /// <summary>All operation records, in call order.</summary>
    public List<ProxyOperation> Operations { get; } = new();

    public void Add(object? item, int index)
    {
        Operations.Add(new ProxyOperation(ProxyOpKind.Add, index, item));
        _items.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        Operations.Add(new ProxyOperation(ProxyOpKind.RemoveAt, index, null));
        if (index >= 0 && index < _items.Count)
            _items.RemoveAt(index);
    }

    public void Move(int oldIndex, int newIndex)
    {
        Operations.Add(new ProxyOperation(ProxyOpKind.Move, oldIndex, newIndex));
        if (oldIndex < 0 || oldIndex >= _items.Count)
            return;
        var item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);
    }

    public void Clear()
    {
        Operations.Add(new ProxyOperation(ProxyOpKind.Clear, 0, null));
        _items.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _items.Clear();
        _disposed = true;
    }
}

/// <summary>Proxy operation kind.</summary>
public enum ProxyOpKind
{
    Add,
    RemoveAt,
    Move,
    Clear,
}

/// <summary>Proxy operation record.</summary>
public sealed record ProxyOperation(ProxyOpKind Kind, int Index, object? Item);

public sealed class StubVirtualizedItemsTargetProxy : IVirtualizedItemsTargetProxy
{
    public IList? Items { get; private set; }
    public int RefreshCount { get; private set; }
    public bool IsDisposed { get; private set; }

    public void SetItems(IList? items) => Items = items;

    public void Refresh() => RefreshCount++;

    public void Dispose()
    {
        Items = null;
        IsDisposed = true;
    }
}
