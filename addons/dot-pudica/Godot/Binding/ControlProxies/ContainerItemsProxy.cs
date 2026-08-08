using System;
using System.Collections.Generic;
using System.Windows.Input;
using DotPudica.Core.Binding;
using DotPudica.Core.ObjectPool;
using DotPudica.Godot.ObjectPool;
using Godot;

namespace DotPudica.Godot.Binding.ControlProxies;

/// <summary>
/// When the item template root implements this, ContainerItemsProxy sets the data item on instantiate.
/// </summary>
public interface IItemsControlItem
{
    /// <summary>The currently bound data item. The implementor should refresh its own UI when set.</summary>
    object? DataContext { get; set; }
}

public interface IItemsControlItemCommand
{
    ICommand? ItemCommand { get; set; }
}

/// <summary>
/// Maps ObservableCollection mutations to AddChild / RemoveChild / MoveChild.
/// Container child order determines display order.
/// </summary>
public sealed class ContainerItemsProxy : IItemsTargetProxy
{
    private readonly Container _container;
    private readonly PackedScene _itemScene;
    private readonly IObjectPool<Node>? _pool;
    private readonly Func<ICommand?>? _getItemCommand;
    private readonly List<Node> _instantiated = new();
    private bool _disposed;

    public ContainerItemsProxy(
        Container container,
        PackedScene itemScene,
        int poolSize = 0,
        Func<ICommand?>? getItemCommand = null)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _itemScene = itemScene ?? throw new ArgumentNullException(nameof(itemScene));
        _pool = poolSize > 0 ? NodePool.Create(_itemScene, poolSize) : null;
        _getItemCommand = getItemCommand;
    }

    public void Add(object? item, int index)
    {
        var node = _pool?.Allocate() ?? _itemScene.Instantiate();
        if (node is null)
            throw new InvalidOperationException("PackedScene.Instantiate returned null, cannot create item node.");

        // Container child order determines display order.
        _container.AddChild(node);
        _instantiated.Insert(index, node);
        _container.MoveChild(node, index);

        if (node is IItemsControlItem itemView)
            itemView.DataContext = item;

        if (node is IItemsControlItemCommand commandView)
            commandView.ItemCommand = _getItemCommand?.Invoke();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _instantiated.Count)
            return;

        var node = _instantiated[index];
        _instantiated.RemoveAt(index);

        if (node is IItemsControlItem itemView)
            itemView.DataContext = null;

        Recycle(node);
    }

    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _instantiated.Count)
            return;
        if (newIndex < 0 || newIndex >= _instantiated.Count)
            return;
        if (oldIndex == newIndex)
            return;

        var node = _instantiated[oldIndex];
        _instantiated.RemoveAt(oldIndex);
        _instantiated.Insert(newIndex, node);
        _container.MoveChild(node, newIndex);
    }

    public void Clear()
    {
        foreach (var node in _instantiated)
        {
            if (node is IItemsControlItem itemView)
                itemView.DataContext = null;

            Recycle(node);
        }
        _instantiated.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Clear();
        _pool?.Dispose();
        _disposed = true;
    }

    private void Recycle(Node node)
    {
        if (_pool is not null)
        {
            _pool.Free(node);
            return;
        }

        _container.RemoveChild(node);
        node.QueueFree();
    }
}
