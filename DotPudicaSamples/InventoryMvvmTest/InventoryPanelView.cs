using System.Collections.Specialized;
using System.ComponentModel;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.InventoryMvvmTest;

[DotPudicaView(typeof(InventoryTestViewModel))]
public abstract partial class InventoryPanelView : Control
{
    [Export]
    private Label _panelTitle = null!;

    [Export]
    private Label _cellSizeText = null!;

    [Export]
    private Control _gridRoot = null!;

    [Export]
    private Button _addButton = null!;

    [Export]
    private Button _removeButton = null!;

    private readonly List<ColorRect> _slotViews = [];
    private readonly Dictionary<int, PanelContainer> _itemViews = [];
    private InventoryTestViewModel? _observedViewModel;
    private int? _selectedItemId;
    private int? _draggingItemId;
    private Vector2 _dragOffset;

    protected abstract SpinBox CellSizeEditor { get; }
    protected abstract int GetCellSize(InventoryTestViewModel vm);
    protected abstract string PanelTitle { get; }

    public override void _Ready()
    {
        DotPudicaInitialize();

        BindingContext.DataContextChanged += OnDataContextChanged;
        _addButton.Pressed += OnAddButtonPressed;
        _removeButton.Pressed += OnRemoveButtonPressed;
        _panelTitle.Text = PanelTitle;
        CellSizeEditor.Step = 1;
        CellSizeEditor.MinValue = 16;
        CellSizeEditor.MaxValue = 96;
        RebindViewModel(ViewModel);
    }

    public override void _ExitTree()
    {
        BindingContext.DataContextChanged -= OnDataContextChanged;
        _addButton.Pressed -= OnAddButtonPressed;
        _removeButton.Pressed -= OnRemoveButtonPressed;
        RebindViewModel(null);
        DotPudicaDispose();
        base._ExitTree();
    }

    public override void _Input(InputEvent @event)
    {
        if (ViewModel == null || !_draggingItemId.HasValue)
            return;

        if (@event is InputEventMouseMotion)
        {
            UpdateDraggingPosition();
            return;
        }

        if (@event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left
            && !mouseButton.Pressed)
        {
            CompleteDrag();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        RebindViewModel(ViewModel);
    }

    private void RebindViewModel(InventoryTestViewModel? next)
    {
        if (_observedViewModel != null)
        {
            _observedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _observedViewModel.Items.CollectionChanged -= OnItemsChanged;
        }

        _observedViewModel = next;

        if (_observedViewModel != null)
        {
            _observedViewModel.PropertyChanged += OnViewModelPropertyChanged;
            _observedViewModel.Items.CollectionChanged += OnItemsChanged;
            RedrawInventory();
        }
        else
        {
            _cellSizeText.Text = "Cell: -";
            ClearGrid();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == null
            || e.PropertyName == nameof(InventoryTestViewModel.ViewACellSize)
            || e.PropertyName == nameof(InventoryTestViewModel.ViewBCellSize)
            || e.PropertyName == nameof(InventoryTestViewModel.SyncCellSize)
            || e.PropertyName == nameof(InventoryTestViewModel.Revision))
        {
            RedrawInventory();
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RedrawInventory();
    }

    private void RedrawInventory()
    {
        if (ViewModel == null)
            return;

        var cellSize = GetCellSize(ViewModel);
        _cellSizeText.Text = $"Cell: {cellSize}px";

        var totalSlots = ViewModel.GridColumns * ViewModel.GridRows;
        EnsureSlotViews(totalSlots);
        EnsureItemViews();

        var gridSize = new Vector2(ViewModel.GridColumns * cellSize, ViewModel.GridRows * cellSize);
        _gridRoot.CustomMinimumSize = gridSize;
        _gridRoot.Size = gridSize;

        for (var index = 0; index < totalSlots; index++)
        {
            var x = index % ViewModel.GridColumns;
            var y = index / ViewModel.GridColumns;
            var slot = _slotViews[index];
            slot.Position = new Vector2(x * cellSize, y * cellSize);
            slot.Size = new Vector2(cellSize - 1, cellSize - 1);
            slot.Color = new Color("202632");
            slot.Show();
        }

        foreach (var item in ViewModel.Items)
        {
            if (!_itemViews.TryGetValue(item.Id, out var itemView))
                continue;

            itemView.Position = new Vector2(item.X * cellSize, item.Y * cellSize);
            itemView.Size = new Vector2(item.Width * cellSize - 2, item.Height * cellSize - 2);
            itemView.Modulate = item.Color;
            itemView.SelfModulate = _selectedItemId == item.Id ? new Color(1f, 1f, 1f, 0.92f) : Colors.White;
            itemView.Show();

            if (itemView.GetChildCount() > 0 && itemView.GetChild(0) is Label label)
            {
                label.Text = item.Name;
                label.AddThemeFontSizeOverride("font_size", ViewModel.CalculateItemFontSize(cellSize, item));
            }
        }

        _removeButton.Disabled = !_selectedItemId.HasValue;
    }

    private void EnsureSlotViews(int count)
    {
        while (_slotViews.Count < count)
        {
            var slot = new ColorRect
            {
                MouseFilter = MouseFilterEnum.Ignore
            };
            _gridRoot.AddChild(slot);
            _slotViews.Add(slot);
        }

        for (var index = 0; index < _slotViews.Count; index++)
            _slotViews[index].Visible = index < count;
    }

    private void EnsureItemViews()
    {
        if (ViewModel == null)
            return;

        var validIds = ViewModel.Items.Select(i => i.Id).ToHashSet();
        var staleIds = _itemViews.Keys.Where(id => !validIds.Contains(id)).ToList();
        foreach (var staleId in staleIds)
        {
            _itemViews[staleId].QueueFree();
            _itemViews.Remove(staleId);
            if (_selectedItemId == staleId)
                _selectedItemId = null;
            if (_draggingItemId == staleId)
                _draggingItemId = null;
        }

        foreach (var item in ViewModel.Items)
        {
            if (_itemViews.ContainsKey(item.Id))
                continue;

            var panel = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Stop,
                ThemeTypeVariation = "Panel"
            };

            var label = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore
            };

            panel.AddChild(label);
            label.SetAnchorsPreset(LayoutPreset.FullRect);
            _gridRoot.AddChild(panel);
            panel.GuiInput += @event => OnItemGuiInput(item.Id, panel, @event);
            _itemViews[item.Id] = panel;
        }
    }

    private void ClearGrid()
    {
        _selectedItemId = null;
        _draggingItemId = null;
        foreach (var slot in _slotViews)
            slot.Hide();
        foreach (var item in _itemViews.Values)
            item.QueueFree();
        _itemViews.Clear();
        _gridRoot.CustomMinimumSize = Vector2.Zero;
        _gridRoot.Size = Vector2.Zero;
        _removeButton.Disabled = true;
    }

    private void OnItemGuiInput(int itemId, PanelContainer panel, InputEvent @event)
    {
        if (ViewModel == null)
            return;

        if (@event is not InputEventMouseButton mouseButton)
            return;

        if (mouseButton.ButtonIndex != MouseButton.Left || !mouseButton.Pressed)
            return;

        _selectedItemId = itemId;
        _draggingItemId = itemId;
        _dragOffset = panel.GetLocalMousePosition();
        panel.ZIndex = 200;
        _removeButton.Disabled = false;
        UpdateDraggingPosition();
    }

    private void UpdateDraggingPosition()
    {
        if (ViewModel == null || !_draggingItemId.HasValue)
            return;
        if (!_itemViews.TryGetValue(_draggingItemId.Value, out var panel))
            return;

        var pointer = _gridRoot.GetLocalMousePosition();
        panel.Position = pointer - _dragOffset;

        var item = ViewModel.Items.FirstOrDefault(it => it.Id == _draggingItemId.Value);
        if (item == null)
            return;

        var cellSize = GetCellSize(ViewModel);
        var target = PixelToCell(panel.Position, cellSize);
        var valid = ViewModel.CanPlaceItem(item.Id, target.X, target.Y, item.Width, item.Height);
        panel.SelfModulate = valid ? new Color(1f, 1f, 1f, 0.92f) : new Color(1f, 0.65f, 0.65f, 0.92f);
    }

    private void CompleteDrag()
    {
        if (ViewModel == null || !_draggingItemId.HasValue)
            return;

        var itemId = _draggingItemId.Value;
        _draggingItemId = null;

        if (!_itemViews.TryGetValue(itemId, out var panel))
            return;

        var item = ViewModel.Items.FirstOrDefault(it => it.Id == itemId);
        if (item == null)
            return;

        var cellSize = GetCellSize(ViewModel);
        var target = PixelToCell(panel.Position, cellSize);
        ViewModel.TryMoveItem(itemId, target.X, target.Y);
        panel.ZIndex = 0;
        RedrawInventory();
    }

    private static Vector2I PixelToCell(Vector2 pixelPosition, int cellSize)
    {
        var x = Mathf.RoundToInt(pixelPosition.X / cellSize);
        var y = Mathf.RoundToInt(pixelPosition.Y / cellSize);
        return new Vector2I(x, y);
    }

    private void OnAddButtonPressed()
    {
        ViewModel?.AddRandomItem();
    }

    private void OnRemoveButtonPressed()
    {
        if (ViewModel == null || !_selectedItemId.HasValue)
            return;

        if (ViewModel.RemoveItem(_selectedItemId.Value))
            _selectedItemId = null;
    }
}
