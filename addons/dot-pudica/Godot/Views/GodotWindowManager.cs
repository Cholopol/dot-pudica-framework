using Godot;
using DotPudica.Core.ObjectPool;
using DotPudica.Godot.ObjectPool;

namespace DotPudica.Godot.Views;

/// <summary>
/// Godot window manager. Owns stack policy, QueuedPopup FIFO, and Full restore.
/// Scene-tree parenting is the host's job (<c>Prepare*</c>); orphan windows fall back to <see cref="AddChild"/>.
/// Manager policy branches only on <see cref="WindowType.Full"/> and <see cref="WindowType.QueuedPopup"/>.
/// </summary>
public partial class GodotWindowManager : Node, IWindowManager
{
    private readonly List<IWindow> _windows = new();
    private readonly Queue<IWindow> _queuedPopups = new();
    private bool _draining;
    private bool _stackChangedDuringDrain;

    private sealed class WindowPoolEntry
    {
        public IObjectPool Pool { get; }
        public int MaxSize { get; }

        public WindowPoolEntry(IObjectPool pool, int maxSize)
        {
            Pool = pool;
            MaxSize = maxSize;
        }
    }

    private readonly Dictionary<Type, WindowPoolEntry> _pools = new();

    /// <inheritdoc />
    public event EventHandler? StackChanged;

    /// <inheritdoc />
    public IWindow? Current => _windows.Count > 0 ? _windows[^1] : null;

    /// <inheritdoc />
    /// <remarks>
    /// Returns a snapshot copy so callers can safely dismiss while enumerating.
    /// </remarks>
    public IReadOnlyList<IWindow> Stack
        => _windows.Count == 0 ? Array.Empty<IWindow>() : _windows.ToArray();

    /// <inheritdoc />
    public int QueuedCount => _queuedPopups.Count;

    /// <summary>
    /// Show window. Handles by WindowType:
    /// - Full: hide previous window (or finish its dismiss)
    /// - Popup / Dialog / Progress: overlay stack (same policy)
    /// - QueuedPopup: enqueue while another QueuedPopup is visible, or while stack top is still a QueuedPopup (incl. hidden)
    /// </summary>
    public ITransition Show(IWindow window, bool ignoreAnimation = false)
    {
        if (window.WindowType == WindowType.QueuedPopup && ShouldEnqueueQueuedPopup())
        {
            _queuedPopups.Enqueue(window);
            RaiseStackChanged();
            return CompletedTransition.Instance;
        }

        // Full: passivate previous window (Hide, or ForceComplete an in-flight Dismiss).
        if (window.WindowType == WindowType.Full &&
            Current is { } previous &&
            !ReferenceEquals(previous, window) &&
            !previous.Dismissed)
        {
            if (previous.IsDismissing)
                previous.Dismiss(ignoreAnimation: true);
            else
                previous.Hide(true);
        }

        window.WindowManager = this;

        window.Create();

        var added = false;
        if (!_windows.Contains(window))
        {
            _windows.Add(window);
            window.WindowDismissed += OnWindowDismissed;
            window.StateChanged += OnWindowStateChanged;
            added = true;
        }

        // Orphan fallback — hosts should Prepare* before Show when they own parenting.
        if (window is Node node && node.GetParent() == null)
            AddChild(node);

        var transition = window.Show(ignoreAnimation);
        if (added)
            RaiseStackChanged();
        return transition;
    }

    /// <inheritdoc />
    public ITransition Hide(IWindow window, bool ignoreAnimation = false)
        => window.Hide(ignoreAnimation);

    /// <inheritdoc />
    public ITransition Dismiss(IWindow window, bool ignoreAnimation = false)
        => window.Dismiss(ignoreAnimation);

    public T? Find<T>() where T : class, IWindow
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i] is T window)
                return window;
        }
        return null;
    }

    /// <inheritdoc />
    public void ConfigurePool<TWindow>(int maxSize) where TWindow : GodotWindow, new()
    {
        if (_pools.TryGetValue(typeof(TWindow), out var existing))
        {
            if (existing.MaxSize != maxSize)
                throw new InvalidOperationException(
                    $"Window pool for {typeof(TWindow).Name} is already configured with maxSize={existing.MaxSize}.");
            return;
        }

        _pools.Add(typeof(TWindow), new WindowPoolEntry((IObjectPool)NodePool.Create<TWindow>(maxSize), maxSize));
    }

    /// <inheritdoc />
    public TWindow ShowPooled<TWindow>(IBundle? bundle = null, bool ignoreAnimation = false)
        where TWindow : GodotWindow, new()
    {
        if (!_pools.TryGetValue(typeof(TWindow), out var entry))
            throw new InvalidOperationException(
                $"No window pool configured for {typeof(TWindow).Name}; call ConfigurePool first.");

        var window = (TWindow)entry.Pool.Allocate();
        window.IsPooled = true;
        if (bundle is not null)
            window.Create(bundle);
        Show(window, ignoreAnimation);
        return window;
    }

    /// <inheritdoc />
    public void Clear(Func<IWindow, bool>? predicate = null)
    {
        var queueChanged = false;
        if (predicate is null)
        {
            while (_queuedPopups.Count > 0)
            {
                ReleaseQueuedWindow(_queuedPopups.Dequeue());
                queueChanged = true;
            }
        }
        else
        {
            var kept = new Queue<IWindow>();
            while (_queuedPopups.Count > 0)
            {
                var next = _queuedPopups.Dequeue();
                if (predicate(next))
                {
                    ReleaseQueuedWindow(next);
                    queueChanged = true;
                }
                else
                    kept.Enqueue(next);
            }

            while (kept.Count > 0)
                _queuedPopups.Enqueue(kept.Dequeue());
        }

        var snapshot = _windows.ToArray();
        for (var i = snapshot.Length - 1; i >= 0; i--)
        {
            var window = snapshot[i];
            if (window.Dismissed)
                continue;
            if (predicate is not null && !predicate(window))
                continue;

            window.Dismiss(ignoreAnimation: true);
        }

        if (queueChanged)
            RaiseStackChanged();

        if (predicate is null)
            DisposePoolEntries();
    }

    private void DisposePoolEntries()
    {
        foreach (var entry in _pools.Values)
            entry.Pool.Dispose();
        _pools.Clear();
    }

    private void OnWindowDismissed(object? sender, EventArgs e)
        => Forget((IWindow)sender!);

    private void OnWindowStateChanged(object? sender, WindowStateEventArgs e)
    {
        // Membership unchanged, but stack entry status (e.g. "closing") changed.
        if (e.NewState == WindowState.DismissBegin)
            RaiseStackChanged();
    }

    internal void Forget(IWindow window)
    {
        window.WindowDismissed -= OnWindowDismissed;
        window.StateChanged -= OnWindowStateChanged;
        if (!_windows.Remove(window))
            return;

        if (window.WindowType == WindowType.Full)
        {
            var previousFull = _windows.FindLast(w => w.WindowType == WindowType.Full);
            if (previousFull != null && !previousFull.IsWindowVisible && !previousFull.Dismissed)
                previousFull.Show();
        }

        ProcessQueuedPopups();
        RaiseStackChanged();
        RecycleIfPooled(window);
    }

    /// <summary>
    /// Recycles a dismissed pooled window: detach → reset → cache; QueueFree when the pool
    /// is gone (manager disposed pools) or full.
    /// </summary>
    private void RecycleIfPooled(IWindow window)
    {
        if (window is not GodotWindow gw || !gw.IsPooled)
            return;
        if (!_pools.TryGetValue(window.GetType(), out var entry))
        {
            gw.QueueFree();
            return;
        }

        if (gw.GetParent() is { } parent)
        {
            parent.RemoveChild(gw);   // _ExitTree → RecycleView（解绑 + 断 VM + RequestReady）
        }
        else
        {
            gw.RequestReady();        // 无父节点：_ExitTree 未触发，兜底重武装
        }
        gw.ResetForReuse();
        entry.Pool.Free(gw);
    }

    private void ProcessQueuedPopups()
    {
        if (_draining)
            return;

        _draining = true;
        try
        {
            while (_queuedPopups.Count > 0)
            {
                if (HasVisibleQueuedPopup())
                    break;

                var next = _queuedPopups.Dequeue();
                if (next.Dismissed)
                    continue;
                if (next is GodotObject godotObj && !GodotObject.IsInstanceValid(godotObj))
                    continue;

                Show(next);
            }
        }
        finally
        {
            _draining = false;
            if (_stackChangedDuringDrain)
            {
                _stackChangedDuringDrain = false;
                RaiseStackChanged();
            }
        }
    }

    /// <summary>
    /// Enqueue when a QueuedPopup is already visible, or when stack top is still a QueuedPopup
    /// (including hidden — preserves FIFO after <see cref="Hide"/>).
    /// </summary>
    private bool ShouldEnqueueQueuedPopup()
        => HasVisibleQueuedPopup()
           || Current is { WindowType: WindowType.QueuedPopup, Dismissed: false };

    private bool HasVisibleQueuedPopup()
        => _windows.Exists(w => w.WindowType == WindowType.QueuedPopup && w.IsWindowVisible);

    private static void ReleaseQueuedWindow(IWindow window)
    {
        if (window.Dismissed)
            return;

        if (window is GodotObject godotObj && !GodotObject.IsInstanceValid(godotObj))
            return;

        if (window.Created || window.IsDismissing)
        {
            window.Dismiss(ignoreAnimation: true);
            return;
        }

        if (window is Node node)
            node.QueueFree();
    }

    private void RaiseStackChanged()
    {
        // Coalesce while draining so StackChanged handlers that Dismiss/Clear/Show
        // cannot re-enter ProcessQueuedPopups and skip remaining queue items.
        if (_draining)
        {
            _stackChangedDuringDrain = true;
            return;
        }

        StackChanged?.Invoke(this, EventArgs.Empty);
    }

    public override void _ExitTree()
    {
        DisposePoolEntries();
        base._ExitTree();
    }
}
