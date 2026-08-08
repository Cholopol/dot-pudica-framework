using System;
using System.Collections;
using System.Windows.Input;
using DotPudica.Core.Binding;
using DotPudica.Core.ObjectPool;
using DotPudica.Godot.ObjectPool;
using Godot;

namespace DotPudica.Godot.Binding.ControlProxies;

/// <summary>
/// Fixed-height, viewport-driven list control. Unlike <see cref="ContainerItemsProxy"/>,
/// it keeps only the visible item window (plus overscan) in the scene tree.
/// </summary>
public partial class VirtualizedItemsControl : ScrollContainer
{
	private readonly Dictionary<int, Control> _activeItems = new();
	private readonly Dictionary<int, object?> _boundItems = new();
	private readonly List<int> _recycleIndices = new();
	private readonly List<int> _newIndices = new();
	private Control? _content;
	private IList? _items;
	private PackedScene? _itemScene;
	private IObjectPool<Node>? _pool;
	private Func<ICommand?>? _itemCommandProvider;
	private VirtualizedItemRange? _renderedRange;
	private int _renderedItemCount = -1;
	private bool _signalsHooked;

	[Export(PropertyHint.Range, "1,4096,1")]
	public float ItemHeight { get; set; } = 32;

	[Export(PropertyHint.Range, "0,32,1")]
	public int Overscan { get; set; } = 1;

	[Export(PropertyHint.Range, "1,1024,1")]
	public int RecycleCapacity { get; set; } = 64;

	/// <summary>Visible viewport item nodes only (excludes recycled).</summary>
	public int ActiveItemCount => _activeItems.Count;

	public override void _Ready()
	{
		EnsureContent();
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		HookSignals();
		// Reparent / on re-entering the tree, _Ready will not run again, so restore the visible viewport.
		if (_itemScene is not null && _items is not null)
			Refresh(rebindData: true, refreshGeometry: true);
	}

	public override void _ExitTree()
	{
		UnhookSignals();
		ClearActiveItems();
		// Only dispose the pool on actual destruction; temporary leaves (e.g., Reparent) must preserve the Configure result.
		if (IsQueuedForDeletion())
		{
			_pool?.Dispose();
			_pool = null;
		}

		base._ExitTree();
	}

	private void HookSignals()
	{
		if (_signalsHooked)
			return;

		GetVScrollBar().ValueChanged += OnScrollChanged;
		Resized += OnResized;
		_signalsHooked = true;
	}

	private void UnhookSignals()
	{
		if (!_signalsHooked)
			return;

		var scroll = GetVScrollBar();
		if (GodotObject.IsInstanceValid(scroll))
			scroll.ValueChanged -= OnScrollChanged;
		Resized -= OnResized;
		_signalsHooked = false;
	}

	/// <summary>
	/// Configures the item scene once before the control receives a collection.
	/// Every scene root must derive from <see cref="Control"/>.
	/// The bound collection can only be modified on the UI thread; background threads should submit snapshots via IUiDispatcher before refreshing.
	/// </summary>
	public void Configure(PackedScene itemScene, Func<ICommand?>? itemCommandProvider = null)
	{
		if (_itemScene is not null)
			throw new InvalidOperationException("Virtualized list item template can only be configured once.");

		_itemScene = itemScene ?? throw new ArgumentNullException(nameof(itemScene));
		_itemCommandProvider = itemCommandProvider;
		_pool = NodePool.Create(_itemScene, RecycleCapacity);
		Refresh(rebindData: true, refreshGeometry: true);
	}

	public void SetItems(IList? items)
	{
		_items = items;
		Refresh(rebindData: true, refreshGeometry: true);
	}

	public void ScrollToIndex(int index)
	{
		var itemCount = _items?.Count ?? 0;
		if (index < 0 || index >= itemCount)
			throw new ArgumentOutOfRangeException(nameof(index));

		GetVScrollBar().Value = index * ItemHeight;
	}

	public void Refresh() => Refresh(rebindData: true, refreshGeometry: false);

	private void Refresh(bool rebindData, bool refreshGeometry)
	{
		if (_content is null)
			return;
		if (_itemScene is null)
			return;

		var itemCount = _items?.Count ?? 0;
		if (_renderedItemCount != itemCount)
		{
			_content.CustomMinimumSize = new Vector2(0, itemCount * ItemHeight);
			_renderedItemCount = itemCount;
		}

		var range = VirtualizedItemRangeCalculator.Calculate(
			itemCount,
			ItemHeight,
			(float)GetVScrollBar().Value,
			Size.Y,
			Overscan);

		_recycleIndices.Clear();
		foreach (var index in _activeItems.Keys)
		{
			if (index < range.StartIndex || index >= range.EndIndex)
				_recycleIndices.Add(index);
		}

		foreach (var index in _recycleIndices)
			Recycle(index);

		_newIndices.Clear();
		for (var index = range.StartIndex; index < range.EndIndex; index++)
		{
			if (EnsureItem(index))
				_newIndices.Add(index);
		}

		if (refreshGeometry)
		{
			foreach (var (index, item) in _activeItems)
				LayoutItem(index, item);
		}
		else
		{
			foreach (var index in _newIndices)
				LayoutItem(index, _activeItems[index]);
		}

		if (rebindData)
		{
			foreach (var (index, item) in _activeItems)
				UpdateItemData(index, item);
		}
		else
		{
			foreach (var index in _newIndices)
				UpdateItemData(index, _activeItems[index]);
		}

		_renderedRange = range;
	}

	private void EnsureContent()
	{
		if (_content is not null)
			return;

		_content = new Control
		{
			Name = "VirtualizedContent",
		};
		AddChild(_content);
	}

	private bool EnsureItem(int index)
	{
		if (_activeItems.ContainsKey(index))
			return false;

		var item = _pool?.Allocate() as Control
			?? throw new InvalidOperationException("Virtualized list item template root node must inherit Control.");

		_content!.AddChild(item);
		_activeItems.Add(index, item);
		return true;
	}

	private void LayoutItem(int index, Control item)
	{
		item.Position = new Vector2(0, index * ItemHeight);
		item.Size = new Vector2(Size.X, ItemHeight);
		item.CustomMinimumSize = new Vector2(0, ItemHeight);
	}

	private void UpdateItemData(int index, Control item)
	{
		if (item is IItemsControlItemCommand commandView)
			commandView.ItemCommand = _itemCommandProvider?.Invoke();

		if (item is not IItemsControlItem itemView)
			return;

		var data = _items![index];
		if (!_boundItems.TryGetValue(index, out var boundData) || !Equals(boundData, data))
		{
			itemView.DataContext = data;
			_boundItems[index] = data;
		}
	}

	private void Recycle(int index)
	{
		var item = _activeItems[index];
		_activeItems.Remove(index);

		if (item is IItemsControlItem itemView)
			itemView.DataContext = null;

		_boundItems.Remove(index);
		_pool!.Free(item);
	}

	private void ClearActiveItems()
	{
		_recycleIndices.Clear();
		foreach (var index in _activeItems.Keys)
			_recycleIndices.Add(index);

		foreach (var index in _recycleIndices)
			Recycle(index);

		_renderedRange = null;
		_renderedItemCount = -1;
	}

	private void OnScrollChanged(double _) => Refresh(rebindData: false, refreshGeometry: false);

	private void OnResized() => Refresh(rebindData: false, refreshGeometry: true);
}

internal sealed class VirtualizedItemsProxy : IVirtualizedItemsTargetProxy
{
	private VirtualizedItemsControl? _target;

	public VirtualizedItemsProxy(
		VirtualizedItemsControl target,
		PackedScene itemScene,
		Func<ICommand?>? getItemCommand = null)
	{
		_target = target ?? throw new ArgumentNullException(nameof(target));
		_target.Configure(itemScene, getItemCommand);
	}

	public void SetItems(IList? items)
	{
		_target?.SetItems(items);
	}

	public void Refresh()
	{
		_target?.Refresh();
	}

	public void Dispose()
	{
		if (_target is null)
			return;

		_target.SetItems(null);
		_target = null;
	}
}
