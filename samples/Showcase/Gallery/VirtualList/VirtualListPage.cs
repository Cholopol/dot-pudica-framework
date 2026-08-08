using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Binding.Converters;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.VirtualList;

/// <summary>
/// Virtual list Gallery: 10,000 items via VirtualizedItemsControl.
/// [ItemsSource] on a VirtualizedItemsControl field generates BindVirtualizedItems — use it for large lists.
/// </summary>
[DotPudicaView(typeof(VirtualListViewModel))]
public partial class VirtualListPage : ShowcasePageWindow
{
    private const string ItemScenePath = "res://samples/Showcase/Gallery/VirtualList/VirtualListItem.tscn";

    [Export, BindTo(nameof(VirtualListViewModel.ItemCount), Converter = typeof(IntToStringConverter))]
    private Label _countLabel = null!;

    [Export, BindTo(nameof(VirtualListViewModel.Status))]
    private Label _statusLabel = null!;

    [Export, BindTo(nameof(VirtualListViewModel.ActiveText))]
    private Label _activeLabel = null!;

    [Export, BindTo(nameof(VirtualListViewModel.RangeText))]
    private Label _rangeLabel = null!;

    [Export, BindCommand(nameof(VirtualListViewModel.InsertAtStartCommand))]
    private Button _insertButton = null!;

    [Export, BindCommand(nameof(VirtualListViewModel.RemoveMiddleCommand))]
    private Button _removeButton = null!;

    [Export, BindCommand(nameof(VirtualListViewModel.MoveFirstToLastCommand))]
    private Button _moveButton = null!;

    [Export, ItemsSource(nameof(VirtualListViewModel.Items), ItemScenePath)]
    private VirtualListItemsControl _virtualList = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_virtualList))
        {
            var bar = _virtualList.GetVScrollBar();
            if (GodotObject.IsInstanceValid(bar))
                bar.ValueChanged -= OnScrollChanged;
            _virtualList.Resized -= OnResized;
        }

        DisposeView();
    }

    partial void OnViewReady() => EnsureControls();

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "10,000 items via VirtualizedItemsControl.");

        var metrics = ShowcaseUi.AddMetricsRow(root);
        ShowcaseUi.AddMetricChip(metrics, "Count", out _countLabel);
        ShowcaseUi.AddMetricChip(metrics, "Active", out _activeLabel);
        ShowcaseUi.AddMetricChip(metrics, "Range", out _rangeLabel);
        ShowcaseUi.AddMetricChip(metrics, "Status", out _statusLabel);

        var actionRow = ShowcaseUi.AddActionRow(root);
        _insertButton = ShowcaseUi.CreatePrimaryButton("Insert Start");
        _removeButton = ShowcaseUi.CreateActionButton("Remove Middle");
        _moveButton = ShowcaseUi.CreateActionButton("Move First→Last");
        actionRow.AddChild(_insertButton);
        actionRow.AddChild(_removeButton);
        actionRow.AddChild(_moveButton);

        _virtualList = new VirtualListItemsControl
        {
            Name = "VirtualList",
            ItemHeight = 36,
            Overscan = 4,
            RecycleCapacity = 48,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(_virtualList);

        _virtualList.GetVScrollBar().ValueChanged += OnScrollChanged;
        _virtualList.Resized += OnResized;
    }

    private void OnScrollChanged(double _) => RefreshVirtualStats();

    private void OnResized() => RefreshVirtualStats();

    private void RefreshVirtualStats()
    {
        if (!GodotObject.IsInstanceValid(_virtualList) || ViewModel is null)
            return;

        var scroll = _virtualList.GetVScrollBar();
        if (!GodotObject.IsInstanceValid(scroll))
            return;

        var first = (int)(scroll.Value / _virtualList.ItemHeight);
        var visible = (int)(_virtualList.Size.Y / _virtualList.ItemHeight);
        ViewModel.UpdateVirtualStats(_virtualList.ActiveItemCount, first, visible);
    }
}
