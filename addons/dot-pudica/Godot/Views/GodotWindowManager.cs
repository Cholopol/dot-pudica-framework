using Godot;

namespace DotPudica.Godot.Views;

/// <summary>
/// Godot window manager. Manages window stack, window lookup and lifecycle control.
/// </summary>
public partial class GodotWindowManager : Node, IWindowManager
{
    private readonly List<IWindow> _windows = new();
    private readonly Queue<IWindow> _queuedPopups = new();
    private bool _isProcessingQueue;

    /// <summary>
    /// Current top window (last opened active window).
    /// </summary>
    public IWindow? Current => _windows.Count > 0 ? _windows[^1] : null;

    /// <summary>
    /// Show window. Handles by WindowType:
    /// - Full: hide previous window
    /// - Popup / Dialog: overlay display
    /// - QueuedPopup: queue and wait
    /// </summary>
    public ITransition Show(IWindow window)
    {
        if (window.WindowType == WindowType.QueuedPopup && Current != null &&
            Current.WindowType == WindowType.QueuedPopup)
        {
            _queuedPopups.Enqueue(window);
            return new GodotTransition((Control)window).DisableAnimation(true);
        }

        // Full type window: passivate previous window
        if (window.WindowType == WindowType.Full && Current != null)
        {
            Current.Hide(true);
        }

        window.WindowManager = this;

        if (!window.Created)
            window.Create();

        _windows.Add(window);

        // If Control node, add to scene tree
        if (window is Node node && node.GetParent() == null)
        {
            AddChild(node);
        }

        var transition = window.Show();

        window.WindowDismissed += OnWindowDismissed;

        return transition;
    }

    /// <summary>
    /// Hide window.
    /// </summary>
    public ITransition Hide(IWindow window)
    {
        return window.Hide();
    }

    /// <summary>
    /// Dismiss window.
    /// </summary>
    public ITransition Dismiss(IWindow window)
    {
        var transition = window.Dismiss();
        return transition;
    }

    /// <summary>
    /// Find window of specified type.
    /// </summary>
    public T? Find<T>() where T : class, IWindow
    {
        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i] is T window)
                return window;
        }
        return null;
    }

    /// <summary>
    /// Close all windows.
    /// </summary>
    public void Clear()
    {
        _queuedPopups.Clear();

        for (int i = _windows.Count - 1; i >= 0; i--)
        {
            _windows[i].Dismiss(ignoreAnimation: true);
        }
        _windows.Clear();
    }

    /// <summary>
    /// Handle when window is dismissed.
    /// </summary>
    private void OnWindowDismissed(object? sender, EventArgs e)
    {
        if (sender is not IWindow window)
            return;

        window.WindowDismissed -= OnWindowDismissed;
        _windows.Remove(window);

        // If top Full window is closed, restore previous Full window
        if (window.WindowType == WindowType.Full)
        {
            var previousFull = _windows.FindLast(w => w.WindowType == WindowType.Full);
            if (previousFull != null && !previousFull.IsWindowVisible)
            {
                previousFull.Show();
            }
        }

        // Process queued popup windows
        ProcessQueuedPopups();
    }

    /// <summary>
    /// Process queued popup windows.
    /// </summary>
    private void ProcessQueuedPopups()
    {
        if (_isProcessingQueue) return;
        _isProcessingQueue = true;

        while (_queuedPopups.Count > 0)
        {
            // Check if any QueuedPopup is currently being displayed
            bool hasActiveQueuedPopup = _windows.Exists(
                w => w.WindowType == WindowType.QueuedPopup && w.IsWindowVisible);

            if (hasActiveQueuedPopup)
                break;

            var next = _queuedPopups.Dequeue();
            Show(next);
        }

        _isProcessingQueue = false;
    }
}
