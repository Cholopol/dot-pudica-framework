namespace DotPudica.Godot.Views;

/// <summary>
/// Window type.
/// </summary>
public enum WindowType
{
    /// <summary>Fullscreen window</summary>
    Full,
    /// <summary>Popup window</summary>
    Popup,
    /// <summary>Dialog</summary>
    Dialog,
    /// <summary>Progress dialog</summary>
    Progress,
    /// <summary>Queued popup window (high priority pops up first)</summary>
    QueuedPopup
}

/// <summary>
/// Window state, defines the complete lifecycle of a window from creation to destruction.
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

/// <summary>
/// Window state change event arguments.
/// </summary>
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

/// <summary>
/// Transition animation interface.
/// </summary>
public interface ITransition
{
    ITransition OnStart(Action callback);
    ITransition OnEnd(Action callback);
    ITransition DisableAnimation(bool disabled);
    Task WaitForFinish();
}

/// <summary>
/// Window attachment data packet.
/// </summary>
public interface IBundle
{
    T Get<T>(string key);
    void Set<T>(string key, T value);
    bool ContainsKey(string key);
}

/// <summary>
/// Window interface.
/// </summary>
public interface IWindow
{
    event EventHandler? WindowVisibilityChanged;
    event EventHandler? WindowActivationChanged;
    event EventHandler? WindowDismissed;
    event EventHandler<WindowStateEventArgs>? StateChanged;

    string WindowName { get; set; }
    bool Created { get; }
    bool Dismissed { get; }
    bool IsWindowVisible { get; }
    bool IsWindowActivated { get; }
    WindowType WindowType { get; set; }
    int WindowPriority { get; set; }
    IWindowManager? WindowManager { get; set; }

    void Create(IBundle? bundle = null);
    ITransition Show(bool ignoreAnimation = false);
    ITransition Hide(bool ignoreAnimation = false);
    ITransition Dismiss(bool ignoreAnimation = false);
}

/// <summary>
/// Window manager interface.
/// </summary>
public interface IWindowManager
{
    IWindow? Current { get; }
    ITransition Show(IWindow window);
    ITransition Hide(IWindow window);
    ITransition Dismiss(IWindow window);
    T? Find<T>() where T : class, IWindow;
    void Clear();
}
