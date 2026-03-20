using Godot;

namespace DotPudica.Godot.Views;

/// <summary>
/// Godot window base class. Adds window lifecycle management on top of Control.
/// Supports the full window state machine: Create -> Show -> Activate -> Passivate -> Hide -> Dismiss.
/// </summary>
public abstract partial class GodotWindow : Control, IWindow
{
    private WindowState _state = WindowState.None;
    private bool _created;
    private bool _dismissed;
    private bool _isVisible;
    private bool _isActivated;

    public event EventHandler? WindowVisibilityChanged;
    public event EventHandler? WindowActivationChanged;
    public event EventHandler? WindowDismissed;
    public event EventHandler<WindowStateEventArgs>? StateChanged;

    public string WindowName { get; set; } = "";
    public bool Created => _created;
    public bool Dismissed => _dismissed;
    public bool IsWindowVisible => _isVisible;
    public bool IsWindowActivated => _isActivated;
    public WindowType WindowType { get; set; } = WindowType.Full;
    public int WindowPriority { get; set; }
    public IWindowManager? WindowManager { get; set; }

    /// <summary>
    /// Current window state.
    /// </summary>
    public WindowState State
    {
        get => _state;
        private set
        {
            if (_state == value)
                return;

            var old = _state;
            _state = value;
            StateChanged?.Invoke(this, new WindowStateEventArgs(this, old, value));
        }
    }

    /// <summary>
    /// Create window.
    /// </summary>
    public void Create(IBundle? bundle = null)
    {
        if (_created)
            return;

        State = WindowState.CreateBegin;
        OnCreate(bundle);
        _created = true;
        State = WindowState.CreateEnd;
    }

    /// <summary>
    /// Show window.
    /// </summary>
    public ITransition Show(bool ignoreAnimation = false)
    {
        if (_dismissed)
            throw new InvalidOperationException("Cannot show a dismissed window.");

        if (!_created)
            Create();

        var transition = new GodotTransition(this);

        transition.OnStart(() =>
        {
            State = WindowState.EnterAnimationBegin;
            Visible = true;
            _isVisible = true;
            WindowVisibilityChanged?.Invoke(this, EventArgs.Empty);
            State = WindowState.Visible;
        });

        transition.OnEnd(() =>
        {
            State = WindowState.EnterAnimationEnd;
            State = WindowState.ActivationAnimationBegin;
            _isActivated = true;
            WindowActivationChanged?.Invoke(this, EventArgs.Empty);
            State = WindowState.Activated;
            State = WindowState.ActivationAnimationEnd;
            OnShow();
        });

        if (ignoreAnimation)
            transition.DisableAnimation(true);

        transition.Execute();
        return transition;
    }

    /// <summary>
    /// Hide window.
    /// </summary>
    public ITransition Hide(bool ignoreAnimation = false)
    {
        var transition = new GodotTransition(this);

        transition.OnStart(() =>
        {
            State = WindowState.PassivationAnimationBegin;
            _isActivated = false;
            WindowActivationChanged?.Invoke(this, EventArgs.Empty);
            State = WindowState.Passivated;
            State = WindowState.PassivationAnimationEnd;
            State = WindowState.ExitAnimationBegin;
        });

        transition.OnEnd(() =>
        {
            Visible = false;
            _isVisible = false;
            WindowVisibilityChanged?.Invoke(this, EventArgs.Empty);
            State = WindowState.Invisible;
            State = WindowState.ExitAnimationEnd;
            OnHide();
        });

        if (ignoreAnimation)
            transition.DisableAnimation(true);

        transition.Execute();
        return transition;
    }

    /// <summary>
    /// Dismiss window.
    /// </summary>
    public ITransition Dismiss(bool ignoreAnimation = false)
    {
        var transition = new GodotTransition(this);

        transition.OnStart(() =>
        {
            if (_isVisible)
            {
                State = WindowState.ExitAnimationBegin;
                Visible = false;
                _isVisible = false;
                _isActivated = false;
                State = WindowState.Invisible;
                State = WindowState.ExitAnimationEnd;
            }

            State = WindowState.DismissBegin;
        });

        transition.OnEnd(() =>
        {
            _dismissed = true;
            OnDismiss();
            State = WindowState.DismissEnd;
            WindowDismissed?.Invoke(this, EventArgs.Empty);
            QueueFree();
        });

        if (ignoreAnimation)
            transition.DisableAnimation(true);

        transition.Execute();
        return transition;
    }

    /// <summary>Called when window is created.</summary>
    protected virtual void OnCreate(IBundle? bundle) { }

    /// <summary>Called after window is shown.</summary>
    protected virtual void OnShow() { }

    /// <summary>Called after window is hidden.</summary>
    protected virtual void OnHide() { }

    /// <summary>Called before window is dismissed.</summary>
    protected virtual void OnDismiss() { }
}

/// <summary>
/// Window bundle data implementation.
/// </summary>
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
