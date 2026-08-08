using Godot;

namespace DotPudica.Godot.Views;

/// <summary>
/// Godot window base class. Adds window lifecycle management on top of Control.
/// Supports Create → Show → Activate → Passivate → Hide → Dismiss.
/// Fine-grained <see cref="WindowState"/> Begin/End markers fire synchronously inside a single
/// transition callback (no separate sub-animations).
/// </summary>
public abstract partial class GodotWindow : Control, IWindow
{
    private WindowState _state = WindowState.None;
    private WindowLifecycle _lifecycle = WindowLifecycle.Uncreated;
    private GodotTransition? _activeTransition;

    public event EventHandler? WindowVisibilityChanged;
    public event EventHandler? WindowActivationChanged;
    public event EventHandler? WindowDismissed;
    public event EventHandler<WindowStateEventArgs>? StateChanged;

    public string WindowName { get; set; } = "";
    public bool Created => _lifecycle is not WindowLifecycle.Uncreated;
    public bool Dismissed => _lifecycle is WindowLifecycle.Dismissed;
    public bool IsWindowVisible => _lifecycle is WindowLifecycle.Visible or WindowLifecycle.Active;
    public bool IsWindowActivated => _lifecycle is WindowLifecycle.Active;
    public WindowType WindowType { get; set; } = WindowType.Full;
    public IWindowManager? WindowManager { get; set; }

    /// <summary>True when allocated from a manager pool: Dismiss recycles the node instead of freeing it.</summary>
    internal bool IsPooled { get; set; }

    /// <summary>
    /// Resets lifecycle to Uncreated (no StateChanged events) so the next Show()/Create() drives cleanly.
    /// _ready re-arming lives in the generated RecycleView (RequestReady).
    /// </summary>
    internal void ResetForReuse()
    {
        _state = WindowState.None;
        _lifecycle = WindowLifecycle.Uncreated;
    }

    /// <summary>True while a dismiss transition is in flight (OnStart done, OnEnd not yet).</summary>
    public bool IsDismissing => State == WindowState.DismissBegin && !Dismissed;

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

    public void Create(IBundle? bundle = null)
    {
        if (Created)
            return;

        State = WindowState.CreateBegin;
        OnCreate(bundle);
        _lifecycle = WindowLifecycle.Hidden;
        State = WindowState.CreateEnd;
    }

    public ITransition Show(bool ignoreAnimation = false)
    {
        if (Dismissed)
            throw new InvalidOperationException("Cannot show a dismissed window.");

        if (IsWindowVisible)
            return CreateCompletedTransition(fadeTarget: 1f);

        AbortActiveTransition();
        if (Dismissed)
            throw new InvalidOperationException("Cannot show a dismissed window.");

        Create();

        var transition = BeginTransition(fadeTarget: 1f);

        transition.OnStart(() =>
        {
            State = WindowState.EnterAnimationBegin;
            Visible = true;
            _lifecycle = WindowLifecycle.Visible;
            WindowVisibilityChanged?.Invoke(this, EventArgs.Empty);
            State = WindowState.Visible;
        });

        transition.OnEnd(() =>
        {
            State = WindowState.EnterAnimationEnd;
            State = WindowState.ActivationAnimationBegin;
            _lifecycle = WindowLifecycle.Active;
            WindowActivationChanged?.Invoke(this, EventArgs.Empty);
            State = WindowState.Activated;
            State = WindowState.ActivationAnimationEnd;
            OnShow();
            ClearActiveTransition(transition);
        });

        if (ignoreAnimation)
            transition.DisableAnimation(true);

        transition.Execute();
        return transition;
    }

    public ITransition Hide(bool ignoreAnimation = false)
    {
        if (Dismissed)
            throw new InvalidOperationException("Cannot hide a dismissed window.");

        if (!IsWindowVisible)
            return CreateCompletedTransition(fadeTarget: 0f);

        AbortActiveTransition();
        if (Dismissed)
            throw new InvalidOperationException("Cannot hide a dismissed window.");

        var transition = BeginTransition(fadeTarget: 0f);

        transition.OnStart(() =>
        {
            State = WindowState.PassivationAnimationBegin;
            _lifecycle = WindowLifecycle.Visible;
            WindowActivationChanged?.Invoke(this, EventArgs.Empty);
            State = WindowState.Passivated;
            State = WindowState.PassivationAnimationEnd;
            State = WindowState.ExitAnimationBegin;
        });

        transition.OnEnd(() =>
        {
            Visible = false;
            _lifecycle = WindowLifecycle.Hidden;
            WindowVisibilityChanged?.Invoke(this, EventArgs.Empty);
            State = WindowState.Invisible;
            State = WindowState.ExitAnimationEnd;
            OnHide();
            ClearActiveTransition(transition);
        });

        if (ignoreAnimation)
            transition.DisableAnimation(true);

        transition.Execute();
        return transition;
    }

    /// <summary>
    /// Dismiss window. Repeated calls reuse the in-flight transition instead of
    /// canceling it (which would drop <see cref="WindowDismissed"/>).
    /// </summary>
    public ITransition Dismiss(bool ignoreAnimation = false)
    {
        if (Dismissed)
            throw new InvalidOperationException("Cannot dismiss a dismissed window.");

        // Already dismissing — do not BeginTransition/Cancel the in-flight OnEnd.
        if (IsDismissing && _activeTransition is not null)
        {
            var active = _activeTransition;
            if (ignoreAnimation)
                active.ForceComplete();
            return active;
        }

        var wasVisible = IsWindowVisible;
        var transition = BeginTransition(fadeTarget: 0f);

        transition.OnStart(() =>
        {
            if (wasVisible)
                State = WindowState.ExitAnimationBegin;

            State = WindowState.DismissBegin;
        });

        transition.OnEnd(() =>
        {
            Visible = false;
            _lifecycle = WindowLifecycle.Dismissed;
            OnDismiss();
            State = WindowState.DismissEnd;
            WindowDismissed?.Invoke(this, EventArgs.Empty);
            ClearActiveTransition(transition);
            if (!IsPooled)
                QueueFree();
        });

        // Already hidden: nothing to fade. Otherwise honor ignoreAnimation.
        if (!wasVisible || ignoreAnimation)
            transition.DisableAnimation(true);

        transition.Execute();
        return transition;
    }

    public override void _ExitTree()
    {
        _activeTransition?.Cancel();
        _activeTransition = null;

        if (WindowManager is GodotWindowManager manager)
            manager.Forget(this);

        base._ExitTree();
    }

    /// <summary>
    /// Drop the in-flight transition. A dismiss in progress is ForceCompleted
    /// (so WindowDismissed still fires); other transitions are canceled.
    /// </summary>
    private void AbortActiveTransition()
    {
        var active = _activeTransition;
        if (active is null)
            return;

        if (IsDismissing)
            active.ForceComplete();
        else
        {
            active.Cancel();
            if (ReferenceEquals(_activeTransition, active))
                _activeTransition = null;
        }
    }

    private GodotTransition BeginTransition(float fadeTarget)
    {
        AbortActiveTransition();
        var transition = new GodotTransition(this).FadeTo(fadeTarget);
        _activeTransition = transition;
        return transition;
    }

    private GodotTransition CreateCompletedTransition(float fadeTarget)
    {
        var transition = new GodotTransition(this).FadeTo(fadeTarget);
        transition.DisableAnimation(true);
        transition.Execute();
        return transition;
    }

    private void ClearActiveTransition(GodotTransition transition)
    {
        if (ReferenceEquals(_activeTransition, transition))
            _activeTransition = null;
    }

    protected virtual void OnCreate(IBundle? bundle) { }

    protected virtual void OnShow() { }

    protected virtual void OnHide() { }

    protected virtual void OnDismiss() { }

    private enum WindowLifecycle
    {
        Uncreated,
        Hidden,
        Visible,
        Active,
        Dismissed
    }
}
