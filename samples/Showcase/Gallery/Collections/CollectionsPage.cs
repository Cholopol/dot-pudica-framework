using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Collections;

/// <summary>
/// Collections Gallery: one ObservableCollection drives two [ItemsSource] containers
/// (PoolSize=0 vs PoolSize=32) to compare node-pool reuse.
/// </summary>
[DotPudicaView(typeof(CollectionsViewModel))]
public partial class CollectionsPage : ShowcasePageWindow
{
    private const string ItemScenePath = "res://samples/Showcase/Gallery/Collections/CollectionItem.tscn";

    private readonly HashSet<Node> _unpooledSeen = new();
    private readonly HashSet<Node> _pooledSeen = new();
    private int _unpooledEntered;
    private int _pooledEntered;
    private int _unpooledExited;
    private int _pooledExited;

    [Export, BindTo(nameof(CollectionsViewModel.StatusText))]
    private Label _statusLabel = null!;

    [Export, BindTo(nameof(CollectionsViewModel.UnpooledStatsText))]
    private Label _unpooledStatsLabel = null!;

    [Export, BindTo(nameof(CollectionsViewModel.PooledStatsText))]
    private Label _pooledStatsLabel = null!;

    [Export, BindCommand(nameof(CollectionsViewModel.AddCommand))]
    private Button _addButton = null!;

    [Export, BindCommand(nameof(CollectionsViewModel.RemoveLastCommand))]
    private Button _removeButton = null!;

    [Export, BindCommand(nameof(CollectionsViewModel.MoveFirstToLastCommand))]
    private Button _moveButton = null!;

    [Export, BindCommand(nameof(CollectionsViewModel.ClearCommand))]
    private Button _clearButton = null!;

    [Export, ItemsSource("Items", ItemScenePath)]
    private VBoxContainer _unpooledList = null!;

    [Export, ItemsSource("Items", ItemScenePath, PoolSize = 32)]
    private VBoxContainer _pooledList = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree()
    {
        UnhookListSignals(_unpooledList, OnUnpooledChildEntered, OnUnpooledChildExited);
        UnhookListSignals(_pooledList, OnPooledChildEntered, OnPooledChildExited);
        DisposeView();
    }

    partial void OnViewReady() => EnsureControls();

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "Same ObservableCollection, PoolSize 0 vs 32.");

        var metrics = ShowcaseUi.AddMetricsRow(root);
        ShowcaseUi.AddMetricChip(metrics, "Status", out _statusLabel);

        var actionRow = ShowcaseUi.AddActionRow(root);
        _addButton = ShowcaseUi.CreatePrimaryButton("Add");
        _removeButton = ShowcaseUi.CreateActionButton("Remove");
        _moveButton = ShowcaseUi.CreateActionButton("Move");
        _clearButton = ShowcaseUi.CreateActionButton("Clear");
        actionRow.AddChild(_addButton);
        actionRow.AddChild(_removeButton);
        actionRow.AddChild(_moveButton);
        actionRow.AddChild(_clearButton);

        var columns = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        columns.AddThemeConstantOverride("separation", 16);
        root.AddChild(columns);

        _unpooledList = BuildColumn(columns, "No Pool", out _unpooledStatsLabel);
        _pooledList = BuildColumn(columns, "Pool 32", out _pooledStatsLabel);

        _unpooledList.ChildEnteredTree += OnUnpooledChildEntered;
        _unpooledList.ChildExitingTree += OnUnpooledChildExited;
        _pooledList.ChildEnteredTree += OnPooledChildEntered;
        _pooledList.ChildExitingTree += OnPooledChildExited;
    }

    private static VBoxContainer BuildColumn(HBoxContainer parent, string title, out Label statsLabel)
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        parent.AddChild(column);

        ShowcaseUi.AddSection(column, title);

        var statsRow = ShowcaseUi.AddMetricsRow(column);
        ShowcaseUi.AddMetricChip(statsRow, "Stats", out statsLabel);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        column.AddChild(scroll);

        var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(list);
        return list;
    }

    private void OnUnpooledChildEntered(Node node)
        => OnChildEntered(node, _unpooledSeen, ref _unpooledEntered, pooled: false);

    private void OnUnpooledChildExited(Node node)
    {
        _unpooledExited++;
        RefreshPoolStats(pooled: false);
    }

    private void OnPooledChildEntered(Node node)
        => OnChildEntered(node, _pooledSeen, ref _pooledEntered, pooled: true);

    private void OnPooledChildExited(Node node)
    {
        _pooledExited++;
        RefreshPoolStats(pooled: true);
    }

    private void OnChildEntered(Node node, HashSet<Node> seen, ref int entered, bool pooled)
    {
        seen.Add(node);
        entered++;
        RefreshPoolStats(pooled);
    }

    private void RefreshPoolStats(bool pooled)
    {
        if (ViewModel is null)
            return;

        var seen = pooled ? _pooledSeen : _unpooledSeen;
        var entered = pooled ? _pooledEntered : _unpooledEntered;
        var exited = pooled ? _pooledExited : _unpooledExited;
        ViewModel.UpdatePoolStats(pooled, alive: entered - exited, created: seen.Count, reused: entered - seen.Count);
    }

    private static void UnhookListSignals(
        VBoxContainer list,
        Node.ChildEnteredTreeEventHandler enteredHandler,
        Node.ChildExitingTreeEventHandler exitedHandler)
    {
        if (list is null || !GodotObject.IsInstanceValid(list))
            return;

        list.ChildEnteredTree -= enteredHandler;
        list.ChildExitingTree -= exitedHandler;
    }
}
