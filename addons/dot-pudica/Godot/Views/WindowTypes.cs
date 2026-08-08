namespace DotPudica.Godot.Views;

/// <summary>
/// Window type. Manager stack policy only branches on <see cref="Full"/> and
/// <see cref="QueuedPopup"/>; <see cref="Popup"/>, <see cref="Dialog"/>, and
/// <see cref="Progress"/> share the same overlay stack behavior (labels only).
/// </summary>
public enum WindowType
{
    /// <summary>Fullscreen page — showing one passivates the previous window; dismissing restores the previous Full.</summary>
    Full,
    /// <summary>Overlay label — stacked above content; same Manager policy as Dialog/Progress.</summary>
    Popup,
    /// <summary>Overlay label — same Manager policy as Popup/Progress.</summary>
    Dialog,
    /// <summary>Overlay label — same Manager policy as Popup/Dialog.</summary>
    Progress,
    /// <summary>Queued overlay — FIFO; only one visible QueuedPopup at a time, next shows after dismiss.</summary>
    QueuedPopup
}

/// <summary>
/// Window state machine markers. Fine-grained Begin/End values are raised synchronously
/// within a single transition callback (no separate sub-animations).
/// </summary>
public enum WindowState
{
    None,
    CreateBegin,
    CreateEnd,
    EnterAnimationBegin,
    Visible,
    EnterAnimationEnd,
    ActivationAnimationBegin,
    Activated,
    ActivationAnimationEnd,
    PassivationAnimationBegin,
    Passivated,
    PassivationAnimationEnd,
    ExitAnimationBegin,
    Invisible,
    ExitAnimationEnd,
    DismissBegin,
    DismissEnd
}

public class WindowStateEventArgs : EventArgs
{
    public IWindow Window { get; }
    public WindowState OldState { get; }
    public WindowState NewState { get; }

    public WindowStateEventArgs(IWindow window, WindowState oldState, WindowState newState)
    {
        Window = window;
        OldState = oldState;
        NewState = newState;
    }
}

public interface ITransition
{
    ITransition OnStart(Action callback);
    ITransition OnEnd(Action callback);
    ITransition DisableAnimation(bool disabled);
    Task WaitForFinish();

    /// <summary>
    /// Abort a running transition and release any engine Callables (e.g. Tween callbacks).
    /// </summary>
    void Cancel();
}

public interface IBundle
{
    T Get<T>(string key);
    void Set<T>(string key, T value);
    bool ContainsKey(string key);
}

public class Bundle : IBundle
{
    private readonly Dictionary<string, object?> _data = new();

    public T Get<T>(string key)
    {
        if (_data.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;

        return default!;
    }

    public void Set<T>(string key, T value)
    {
        _data[key] = value;
    }

    public bool ContainsKey(string key) => _data.ContainsKey(key);
}

public interface IWindow
{
    event EventHandler? WindowVisibilityChanged;
    event EventHandler? WindowActivationChanged;
    event EventHandler? WindowDismissed;
    event EventHandler<WindowStateEventArgs>? StateChanged;

    string WindowName { get; set; }
    bool Created { get; }
    bool Dismissed { get; }
    /// <summary>True while a dismiss transition is in flight (started, not yet finished).</summary>
    bool IsDismissing { get; }
    bool IsWindowVisible { get; }
    bool IsWindowActivated { get; }
    WindowType WindowType { get; set; }
    IWindowManager? WindowManager { get; set; }

    void Create(IBundle? bundle = null);
    ITransition Show(bool ignoreAnimation = false);
    ITransition Hide(bool ignoreAnimation = false);
    ITransition Dismiss(bool ignoreAnimation = false);
}

public interface IWindowManager
{
    /// <summary>
    /// Raised when the live stack or QueuedPopup wait queue changes (show/enqueue/forget/clear),
    /// and when a stacked window enters <see cref="WindowState.DismissBegin"/> (dismiss in flight).
    /// </summary>
    event EventHandler? StackChanged;

    /// <summary>
    /// Stack top (last entry). Not necessarily the current Full page or the only visible window.
    /// </summary>
    IWindow? Current { get; }

    /// <summary>
    /// Bottom-to-top snapshot of the live window stack (safe to enumerate while dismissing).
    /// </summary>
    IReadOnlyList<IWindow> Stack { get; }

    /// <summary>QueuedPopup entries waiting to be shown.</summary>
    int QueuedCount { get; }

    ITransition Show(IWindow window, bool ignoreAnimation = false);

    /// <summary>Forwards to <see cref="IWindow.Hide"/>; does not alter the stack.</summary>
    ITransition Hide(IWindow window, bool ignoreAnimation = false);

    /// <summary>
    /// Forwards to <see cref="IWindow.Dismiss"/>; stack removal happens via
    /// <see cref="IWindow.WindowDismissed"/> / Forget, not inside this call.
    /// </summary>
    ITransition Dismiss(IWindow window, bool ignoreAnimation = false);

    T? Find<T>() where T : class, IWindow;

    /// <summary>
    /// Dismiss matching windows (and matching queued popups). Null predicate clears everything.
    /// </summary>
    void Clear(Func<IWindow, bool>? predicate = null);

    /// <summary>
    /// Registers a per-type window pool. Idempotent for the same capacity; throws for a different one.
    /// Pooled windows recycle on Dismiss; pool overflow destroys them.
    /// </summary>
    void ConfigurePool<TWindow>(int maxSize) where TWindow : GodotWindow, new();

    /// <summary>Shows a pooled window, reusing the cached node when available. Requires <see cref="ConfigurePool{TWindow}"/> first.</summary>
    TWindow ShowPooled<TWindow>(IBundle? bundle = null, bool ignoreAnimation = false)
        where TWindow : GodotWindow, new();
}
