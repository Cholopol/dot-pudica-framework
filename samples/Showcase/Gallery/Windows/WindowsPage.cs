using System.Text;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>
/// Windows — step-through overlay demos; metrics live in the lab strip above.
/// </summary>
[DotPudicaView(typeof(WindowsViewModel))]
public partial class WindowsPage : ShowcasePageWindow
{
    private int _queuedSequence;
    private GodotWindowManager? _windowManager;

    [Export, BindCommand(nameof(WindowsViewModel.OpenPopupCommand))]
    private Button _openPopupButton = null!;

    [Export, BindCommand(nameof(WindowsViewModel.OpenDialogCommand))]
    private Button _openDialogButton = null!;

    [Export, BindCommand(nameof(WindowsViewModel.OpenProgressCommand))]
    private Button _openProgressButton = null!;

    [Export, BindCommand(nameof(WindowsViewModel.EnqueueQueuedPopupsCommand))]
    private Button _enqueueButton = null!;

    [Export, BindCommand(nameof(WindowsViewModel.OpenNestedOverlaysCommand))]
    private Button _nestedButton = null!;

    [Export, BindCommand(nameof(WindowsViewModel.OpenFullContrastCommand))]
    private Button _fullContrastButton = null!;

    [Export, BindCommand(nameof(WindowsViewModel.FindDialogCommand))]
    private Button _findButton = null!;

    [Export, BindCommand(nameof(WindowsViewModel.ClearWindowsCommand))]
    private Button _clearButton = null!;

    [Export, BindTo(nameof(WindowsViewModel.PooledStatsText))]
    private Label _pooledStatsLabel = null!;

    [Export, BindCommand(nameof(WindowsViewModel.AllocatePooledCommand))]
    private Button _openPooledButton = null!;

    [Export, BindCommand(nameof(WindowsViewModel.FreePooledCommand))]
    private Button _dismissPooledButton = null!;

    private readonly HashSet<GodotWindow> _pooledSeen = new();
    private int _pooledCreateCount;
    private int _pooledReuseCount;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady()
    {
        EnsureControls();
        WireLabHudActions();
        RequireWindowManager().ConfigurePool<DemoPooledPopupWindow>(maxSize: 2);
    }

    partial void OnViewModelBound()
    {
        WindowVisibilityChanged += OnPageVisibilityChanged;
        _windowManager = RequireWindowManager();
        _windowManager.StackChanged += OnStackChanged;
        RefreshUi();
    }

    partial void OnViewDisposing()
    {
        WindowVisibilityChanged -= OnPageVisibilityChanged;
        RequireWindowManager().Clear(w => w is DemoPooledPopupWindow);
        if (_windowManager is not null)
        {
            _windowManager.StackChanged -= OnStackChanged;
            _windowManager = null;
        }
    }

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "Step through overlays. Metrics stay in the lab strip above.");
        ShowcaseUi.AddSection(root, "Steps");

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 8);
        root.AddChild(col);

        _openPopupButton = ShowcaseUi.CreateActionButton("1. Popup", expand: true);
        col.AddChild(_openPopupButton);
        _openDialogButton = ShowcaseUi.CreateActionButton("2. Dialog", expand: true);
        col.AddChild(_openDialogButton);
        _openProgressButton = ShowcaseUi.CreateActionButton("3. Progress", expand: true);
        col.AddChild(_openProgressButton);
        _enqueueButton = ShowcaseUi.CreateActionButton("4. Queued Popup ×3", expand: true);
        col.AddChild(_enqueueButton);
        _nestedButton = ShowcaseUi.CreateActionButton("5. Nested Overlay", expand: true);
        col.AddChild(_nestedButton);
        _fullContrastButton = ShowcaseUi.CreateActionButton("6. Full Page Contrast", expand: true);
        col.AddChild(_fullContrastButton);
        _openPooledButton = ShowcaseUi.CreateActionButton("7. Pooled Popup (recycles node)", expand: true);
        col.AddChild(_openPooledButton);
        _dismissPooledButton = ShowcaseUi.CreateActionButton("Dismiss Last Pooled", expand: true);
        col.AddChild(_dismissPooledButton);
        _pooledStatsLabel = new Label { Modulate = ShowcaseTheme.Muted };
        col.AddChild(_pooledStatsLabel);
    }

    /// <summary>Toolbar buttons sit outside OverlayHost so overlays cannot block them.</summary>
    private void WireLabHudActions()
    {
        var hud = ShowcaseLayout.LabHud
            ?? throw new InvalidOperationException("ShowcaseLayout.LabHud must be set before WindowsPage binds.");

        _findButton = hud.FindDialogButton;
        _clearButton = hud.ClearOverlaysButton;
    }

    [Subscribe("OpenPopupRequest.Raised")]
    private void OnOpenPopupRequested(object? sender, EventArgs e)
    {
        BeginScenario("Popup covers content; Status stays Running — dismiss when done.");
        ShowOverlay(new DemoPopupWindow());
    }

    [Subscribe("OpenDialogRequest.Raised")]
    private void OnOpenDialogRequested(object? sender, EventArgs e)
    {
        BeginScenario("Dialog — OK/Cancel updates Dialog in the lab strip.");
        ShowOverlay(new DemoDialogWindow());
    }

    [Subscribe("OpenProgressRequest.Raised")]
    private void OnOpenProgressRequested(object? sender, EventArgs e)
    {
        BeginScenario("Progress auto-closes at 100%; Status stays Running.");
        ShowOverlay(new DemoProgressWindow());
    }

    [Subscribe("EnqueueQueuedPopupsRequest.Raised")]
    private void OnEnqueueQueuedPopupsRequested(object? sender, EventArgs e)
    {
        BeginScenario("Three queued popups — one visible at a time; Stack shows the wait count.");
        for (var i = 0; i < 3; i++)
        {
            _queuedSequence++;
            ShowOverlay(new DemoQueuedPopupWindow(_queuedSequence));
        }
    }

    [Subscribe("OpenNestedOverlaysRequest.Raised")]
    private void OnOpenNestedOverlaysRequested(object? sender, EventArgs e)
    {
        BeginScenario("Dialog then popup — dismiss the top layer first; Dialog remains.");
        ShowOverlay(new DemoDialogWindow());
        ShowOverlay(new DemoPopupWindow("Top layer — dismiss me first; Dialog stays below."));
    }

    [Subscribe("OpenFullContrastRequest.Raised")]
    private void OnOpenFullContrastRequested(object? sender, EventArgs e)
    {
        ClearOverlays();
        ViewModel?.SetGuide("Full page open — Status should read Hidden; dismiss to return to Running.");
        ShowFullPage(new DemoFullContrastPage());
        RefreshUi();
    }

    [Subscribe("FindDialogRequest.Raised")]
    private void OnFindDialogRequested(object? sender, EventArgs e)
    {
        var found = RequireWindowManager().Find<DemoDialogWindow>();
        ViewModel?.SetGuide(found is null
            ? "Find: no Dialog on stack — try step 2 first."
            : "Find: live Dialog on stack.");
        SyncLabHud();
    }

    [Subscribe("ClearWindowsRequest.Raised")]
    private void OnClearWindowsRequested(object? sender, EventArgs e)
    {
        ClearOverlays();
        ViewModel?.SetGuide("Overlays cleared — restart from step 1.");
        SyncLabHud();
    }

    [Subscribe("AllocatePooledRequest.Raised")]
    private void OnAllocatePooledRequested(object? sender, EventArgs e)
    {
        var window = RequireWindowManager().ShowPooled<DemoPooledPopupWindow>();
        if (_pooledSeen.Add(window))
            _pooledCreateCount++;
        else
            _pooledReuseCount++;
    }

    [Subscribe("FreePooledRequest.Raised")]
    private void OnFreePooledRequested(object? sender, EventArgs e)
    {
        var manager = RequireWindowManager();
        if (manager.Find<DemoPooledPopupWindow>() is { Dismissed: false } window)
            manager.Dismiss(window, ignoreAnimation: true);
    }

    private void RefreshPooledStats()
    {
        var manager = RequireWindowManager();
        var live = manager.Stack.Count(w => w is DemoPooledPopupWindow && !w.Dismissed);
        ViewModel?.UpdatePooledStats(live, _pooledCreateCount, _pooledReuseCount);
    }

    /// <summary>Clear the stage before each step so rapid clicks stay readable.</summary>
    private void BeginScenario(string guide)
    {
        ClearOverlays();
        ViewModel?.SetGuide(guide);
        SyncLabHud();
    }

    private void ClearOverlays()
        => RequireWindowManager().Clear(ShouldClearOverlay);

    /// <summary>
    /// Keep this page; dismiss overlays and the Full-contrast demo page.
    /// </summary>
    private static bool ShouldClearOverlay(IWindow window)
    {
        if (window is WindowsPage)
            return false;
        if (window is DemoFullContrastPage)
            return true;
        return window.WindowType != WindowType.Full;
    }

    /// <summary>
    /// Show an overlay via WindowManager. OverlayHost attach is a one-shot on CreateEnd
    /// (so QueuedPopup also lands correctly when dequeued). Dialog results use a one-shot dismiss hook.
    /// </summary>
    private void ShowOverlay(GodotWindow window)
    {
        AttachOverlayHostOnCreate(window);
        if (window is DemoDialogWindow dialog)
            AttachDialogResultOnce(dialog);

        RequireWindowManager().Show(window);
        RefreshUi();
    }

    private static void AttachOverlayHostOnCreate(GodotWindow window)
    {
        void OnStateChanged(object? sender, WindowStateEventArgs e)
        {
            if (e.NewState != WindowState.CreateEnd)
                return;
            if (e.Window is GodotWindow { WindowType: not WindowType.Full } gw)
                ShowcaseLayout.PrepareOverlay(gw);
            window.StateChanged -= OnStateChanged;
        }

        window.StateChanged += OnStateChanged;
    }

    private void AttachDialogResultOnce(DemoDialogWindow dialog)
    {
        void OnDismissed(object? sender, EventArgs e)
        {
            dialog.WindowDismissed -= OnDismissed;
            ViewModel?.SetDialogResult(dialog.Result);
            RefreshUi();
        }

        dialog.WindowDismissed += OnDismissed;
    }

    private void OnPageVisibilityChanged(object? sender, EventArgs e) => RefreshUi();

    private void OnStackChanged(object? sender, EventArgs e) => RefreshUi();

    private void RefreshUi()
    {
        if (ViewModel is null)
            return;

        RefreshPageStatus();
        ViewModel.UpdateStack(BuildStackDump());
        SyncLabHud();
        RefreshPooledStats();
    }

    private void RefreshPageStatus()
    {
        if (ViewModel is null)
            return;

        if (!IsWindowVisible)
        {
            ViewModel.SetPageStatus("Hidden (Full page took over)");
            return;
        }

        var covered = RequireWindowManager().Stack.Any(w =>
            !w.Dismissed
            && w.Created
            && w.IsWindowVisible
            && w.WindowType is WindowType.Popup or WindowType.Dialog or WindowType.Progress or WindowType.QueuedPopup);

        ViewModel.SetPageStatus(covered
            ? "Running · overlay blocking clicks"
            : "Running");
    }

    private void SyncLabHud()
    {
        var hud = ShowcaseLayout.LabHud;
        if (hud is null || ViewModel is null)
            return;

        hud.SetPageStatus(ViewModel.PageStatusText);
        hud.SetGuide(ViewModel.GuideText);
        hud.SetResult(ViewModel.LastResultText);
        hud.SetStack(ViewModel.StackText);
    }

    private string BuildStackDump()
    {
        var wm = RequireWindowManager();
        var current = wm.Current;
        var sb = new StringBuilder();

        if (current is null || current is WindowsPage)
            sb.AppendLine("Top: this page (no overlays)");
        else
            sb.AppendLine($"Top: {Describe(current)}");

        var lines = 0;
        foreach (var w in wm.Stack)
        {
            if (w.Dismissed || w is WindowsPage)
                continue;

            lines++;
            var status = w.IsDismissing
                ? "closing"
                : w.IsWindowVisible ? "visible" : "hidden";
            var mark = ReferenceEquals(w, current) ? " ← top" : "";
            sb.AppendLine($"  · {Describe(w)} ({status}){mark}");
        }

        var waiting = wm.QueuedCount;
        if (lines == 0 && waiting == 0)
            sb.Append("Stack: empty");
        else if (waiting > 0)
            sb.Append($"Queued: {waiting} waiting");

        return sb.ToString().TrimEnd();
    }

    private static string Describe(IWindow window) => window.WindowType switch
    {
        WindowType.Popup => "Popup",
        WindowType.Dialog => "Dialog",
        WindowType.Progress => "Progress",
        WindowType.QueuedPopup => string.IsNullOrEmpty(window.WindowName)
            ? "Queued popup"
            : window.WindowName,
        WindowType.Full => string.IsNullOrEmpty(window.WindowName)
            ? "Full page"
            : $"Full page ({window.WindowName})",
        _ => window.GetType().Name
    };
}
