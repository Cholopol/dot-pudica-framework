using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.ObjectPool;
using DotPudica.Godot.ObjectPool;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Pools;

/// <summary>
/// Pools — <see cref="DotPudica.Core.ObjectPool.ObjectPool{T}"/> + <see cref="IObjectFactory{T}"/>
/// and <see cref="NodePool"/> vs direct <c>new</c> (no MixedObjectPool).
/// Node allocation stays in the View; CLR pool stats live in <see cref="PoolsViewModel"/>.
/// </summary>
[DotPudicaView(typeof(PoolsViewModel))]
public partial class PoolsPage : ShowcasePageWindow
{
    private IObjectPool<Label>? _nodePool;
    private readonly Stack<(Label Label, bool FromPool)> _allocatedLabels = new();
    private VBoxContainer _labelDisplay = null!;
    private int _directCreateCount;
    private int _directDestroyCount;

    private IObjectPool<PooledDetailPanel>? _viewPool;
    private CountingNodeFactory<PooledDetailPanel>? _viewFactory;
    private readonly Stack<(PooledDetailPanel Panel, PooledDetailViewModel Vm)> _allocatedPanels = new();
    private readonly HashSet<PooledDetailPanel> _seenPanels = new();
    private VBoxContainer _panelDisplay = null!;
    private int _viewCreateCount;
    private int _viewReuseCount;
    private int _viewIndex;

    private IObjectPool<PooledAutoInitDemoPanel>? _autoViewPool;
    private CountingNodeFactory<PooledAutoInitDemoPanel>? _autoViewFactory;
    private readonly Stack<PooledAutoInitDemoPanel> _allocatedAutoPanels = new();
    private readonly HashSet<PooledAutoInitDemoPanel> _seenAutoPanels = new();
    private VBoxContainer _autoPanelDisplay = null!;
    private int _autoViewCreateCount;
    private int _autoViewReuseCount;

    [Export, BindTo(nameof(PoolsViewModel.CoreStatsText))]
    private Label _coreStatsLabel = null!;

    [Export, BindCommand(nameof(PoolsViewModel.AllocateCoreCommand))]
    private Button _allocateCoreButton = null!;

    [Export, BindCommand(nameof(PoolsViewModel.FreeCoreCommand))]
    private Button _freeCoreButton = null!;

    [Export, BindTo(nameof(PoolsViewModel.UseNodePool), Mode = BindingMode.TwoWay)]
    private CheckButton _useNodePoolToggle = null!;

    [Export, BindCommand(nameof(PoolsViewModel.AllocateNodeCommand))]
    private Button _allocateNodeButton = null!;

    [Export, BindCommand(nameof(PoolsViewModel.FreeNodeCommand))]
    private Button _freeNodeButton = null!;

    [Export, BindTo(nameof(PoolsViewModel.NodeStatsText))]
    private Label _nodeStatsLabel = null!;

    [Export, BindTo(nameof(PoolsViewModel.PooledStatsText))]
    private Label _pooledStatsLabel = null!;

    [Export, BindCommand(nameof(PoolsViewModel.AllocatePooledCommand))]
    private Button _allocatePooledButton = null!;

    [Export, BindCommand(nameof(PoolsViewModel.FreePooledCommand))]
    private Button _freePooledButton = null!;

    [Export, BindTo(nameof(PoolsViewModel.AutoPooledStatsText))]
    private Label _autoPooledStatsLabel = null!;

    [Export, BindCommand(nameof(PoolsViewModel.AllocateAutoPooledCommand))]
    private Button _allocateAutoPooledButton = null!;

    [Export, BindCommand(nameof(PoolsViewModel.FreeAutoPooledCommand))]
    private Button _freeAutoPooledButton = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    partial void OnViewDisposing()
    {
        while (_allocatedPanels.TryPop(out var entry))
            entry.Vm.Dispose();

        _viewPool?.Dispose();
        _viewPool = null;
        _viewFactory = null;

        _autoViewPool?.Dispose();
        _autoViewPool = null;
        _autoViewFactory = null;
    }

    [Subscribe("AllocateNodeRequest.Raised")]
    private void OnAllocateNodeRequested(object? sender, EventArgs e)
    {
        var fromPool = ViewModel!.UseNodePool;
        Label label;
        if (fromPool)
        {
            _nodePool ??= NodePool.Create<Label>(maxSize: 8);
            label = _nodePool.Allocate();
        }
        else
        {
            label = new Label();
            _directCreateCount++;
        }

        label.Text = $"Node #{_allocatedLabels.Count + 1} ({(fromPool ? "NodePool" : "direct new")})";
        _allocatedLabels.Push((label, fromPool));
        _labelDisplay.AddChild(label);
        RefreshNodeStats();
    }

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "CLR ObjectPool<T> vs Godot NodePool (maxSize=8 each).");

        BuildCoreCard(root);
        BuildNodeCard(root);
        BuildPooledViewCard(root);
        BuildAutoInitViewCard(root);
    }

    private void BuildCoreCard(VBoxContainer root)
    {
        var cardBody = ShowcaseUi.CreateCardBody(out var card);
        root.AddChild(card);

        ShowcaseUi.AddSection(cardBody, "CLR ObjectPool<PoolItem>");

        var metrics = ShowcaseUi.AddMetricsRow(cardBody);
        ShowcaseUi.AddMetricChip(metrics, "Stats", out _coreStatsLabel);

        var actionRow = ShowcaseUi.AddActionRow(cardBody);
        _allocateCoreButton = ShowcaseUi.CreatePrimaryButton("Allocate");
        _freeCoreButton = ShowcaseUi.CreateActionButton("Free Last");
        actionRow.AddChild(_allocateCoreButton);
        actionRow.AddChild(_freeCoreButton);
    }

    private void BuildNodeCard(VBoxContainer root)
    {
        var cardBody = ShowcaseUi.CreateCardBody(out var card);
        cardBody.SizeFlagsVertical = SizeFlags.ExpandFill;
        card.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(card);

        ShowcaseUi.AddSection(cardBody, "NodePool<Label>");

        _useNodePoolToggle = new CheckButton
        {
            Text = "Use NodePool (off = new Label each time)",
            ButtonPressed = true,
        };
        cardBody.AddChild(_useNodePoolToggle);

        var metrics = ShowcaseUi.AddMetricsRow(cardBody);
        ShowcaseUi.AddMetricChip(metrics, "Stats", out _nodeStatsLabel);

        var actionRow = ShowcaseUi.AddActionRow(cardBody);
        _allocateNodeButton = ShowcaseUi.CreatePrimaryButton("Allocate");
        _freeNodeButton = ShowcaseUi.CreateActionButton("Free Last");
        actionRow.AddChild(_allocateNodeButton);
        actionRow.AddChild(_freeNodeButton);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 120),
        };
        cardBody.AddChild(scroll);
        _labelDisplay = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_labelDisplay);
    }

    private void BuildPooledViewCard(VBoxContainer root)
    {
        var cardBody = ShowcaseUi.CreateCardBody(out var card);
        cardBody.SizeFlagsVertical = SizeFlags.ExpandFill;
        card.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(card);

        ShowcaseUi.AddSection(cardBody, "Pooled MVVM View (NodePool<PooledDetailPanel>)");

        var metrics = ShowcaseUi.AddMetricsRow(cardBody);
        ShowcaseUi.AddMetricChip(metrics, "Stats", out _pooledStatsLabel);

        var actionRow = ShowcaseUi.AddActionRow(cardBody);
        _allocatePooledButton = ShowcaseUi.CreatePrimaryButton("Allocate");
        _freePooledButton = ShowcaseUi.CreateActionButton("Free Last");
        actionRow.AddChild(_allocatePooledButton);
        actionRow.AddChild(_freePooledButton);

        var hint = new Label
        {
            Text = "Each Allocate creates a fresh ViewModel; the panel node is pooled ([DotPudicaView(Pooled = true)]). " +
                   "The pool caches at most maxSize=3 recycled panels — freeing a 4th panel destroys the excess node (see destroyed count). " +
                   "So recycling 5 panels and re-adding reuses only the 3 survivors.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        cardBody.AddChild(hint);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 140),
        };
        cardBody.AddChild(scroll);
        _panelDisplay = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_panelDisplay);
    }

    private void BuildAutoInitViewCard(VBoxContainer root)
    {
        var cardBody = ShowcaseUi.CreateCardBody(out var card);
        cardBody.SizeFlagsVertical = SizeFlags.ExpandFill;
        card.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(card);

        ShowcaseUi.AddSection(cardBody, "Auto-Init Pooled View (NodePool<PooledAutoInitDemoPanel>)");

        var metrics = ShowcaseUi.AddMetricsRow(cardBody);
        ShowcaseUi.AddMetricChip(metrics, "Stats", out _autoPooledStatsLabel);

        var actionRow = ShowcaseUi.AddActionRow(cardBody);
        _allocateAutoPooledButton = ShowcaseUi.CreatePrimaryButton("Allocate");
        _freeAutoPooledButton = ShowcaseUi.CreateActionButton("Free Last");
        actionRow.AddChild(_allocateAutoPooledButton);
        actionRow.AddChild(_freeAutoPooledButton);

        var hint = new Label
        {
            Text = "[DotPudicaView(Pooled = true)] + AutoInitialize=true: each Allocate+AddChild re-runs _Ready → " +
                   "InitializeView (fresh Owned ViewModel + bindings); RemoveChild → RecycleView releases it and " +
                   "re-arms _ready for the next entry. Same contract as pooled windows. " +
                   "Freeing a 4th panel destroys the excess node (see destroyed count).",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        cardBody.AddChild(hint);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 140),
        };
        cardBody.AddChild(scroll);
        _autoPanelDisplay = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_autoPanelDisplay);
    }

    [Subscribe("FreeNodeRequest.Raised")]
    private void OnFreeNodeRequested(object? sender, EventArgs e)
    {
        if (!_allocatedLabels.TryPop(out var entry))
            return;

        if (entry.FromPool && _nodePool is not null)
        {
            _nodePool.Free(entry.Label);
        }
        else
        {
            entry.Label.GetParent()?.RemoveChild(entry.Label);
            entry.Label.QueueFree();
            _directDestroyCount++;
        }

        RefreshNodeStats();
    }

    private void RefreshNodeStats()
        => ViewModel?.UpdateNodeStats(_allocatedLabels.Count, _directCreateCount, _directDestroyCount);

    [Subscribe("AllocatePooledRequest.Raised")]
    private void OnAllocatePooledRequested(object? sender, EventArgs e)
    {
        _viewPool ??= new ObjectPool<PooledDetailPanel>(
            _viewFactory ??= new CountingNodeFactory<PooledDetailPanel>(), initialSize: 0, maxSize: 3);

        var panel = _viewPool.Allocate();
        if (_seenPanels.Add(panel))
            _viewCreateCount++;
        else
            _viewReuseCount++;

        var vm = new PooledDetailViewModel(++_viewIndex);
        _allocatedPanels.Push((panel, vm));
        _panelDisplay.AddChild(panel);
        panel.BindShared(vm);
        RefreshPooledStats();
    }

    [Subscribe("FreePooledRequest.Raised")]
    private void OnFreePooledRequested(object? sender, EventArgs e)
    {
        if (!_allocatedPanels.TryPop(out var entry))
            return;

        entry.Panel.GetParent()?.RemoveChild(entry.Panel);
        entry.Vm.Dispose();
        _viewPool?.Free(entry.Panel);
        RefreshPooledStats();
    }

    [Subscribe("AllocateAutoPooledRequest.Raised")]
    private void OnAllocateAutoPooledRequested(object? sender, EventArgs e)
    {
        _autoViewPool ??= new ObjectPool<PooledAutoInitDemoPanel>(
            _autoViewFactory ??= new CountingNodeFactory<PooledAutoInitDemoPanel>(), initialSize: 0, maxSize: 3);

        var panel = _autoViewPool.Allocate();
        if (_seenAutoPanels.Add(panel))
            _autoViewCreateCount++;
        else
            _autoViewReuseCount++;

        _allocatedAutoPanels.Push(panel);
        _autoPanelDisplay.AddChild(panel);
        RefreshAutoPooledStats();
    }

    [Subscribe("FreeAutoPooledRequest.Raised")]
    private void OnFreeAutoPooledRequested(object? sender, EventArgs e)
    {
        if (!_allocatedAutoPanels.TryPop(out var panel))
            return;

        panel.GetParent()?.RemoveChild(panel);
        _autoViewPool?.Free(panel);
        RefreshAutoPooledStats();
    }

    private void RefreshAutoPooledStats()
        => ViewModel?.UpdateAutoPooledStats(
            _allocatedAutoPanels.Count,
            _autoViewCreateCount,
            _autoViewReuseCount,
            _autoViewFactory?.DestroyedCount ?? 0);

    private void RefreshPooledStats()
        => ViewModel?.UpdatePooledStats(
            _allocatedPanels.Count,
            _viewCreateCount,
            _viewReuseCount,
            _viewFactory?.DestroyedCount ?? 0);
}
