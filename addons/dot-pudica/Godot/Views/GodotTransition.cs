using Godot;

namespace DotPudica.Godot.Views;

/// <summary>
/// Transition animation implementation based on Godot Tween.
/// </summary>
public class GodotTransition : ITransition
{
    private readonly Control _target;
    private Action? _onStart;
    private Action? _onEnd;
    private bool _animationDisabled;
    private TaskCompletionSource? _tcs;

    public GodotTransition(Control target)
    {
        _target = target;
    }

    public ITransition OnStart(Action callback)
    {
        _onStart = callback;
        return this;
    }

    public ITransition OnEnd(Action callback)
    {
        _onEnd = callback;
        return this;
    }

    public ITransition DisableAnimation(bool disabled)
    {
        _animationDisabled = disabled;
        return this;
    }

    public Task WaitForFinish()
    {
        _tcs ??= new TaskCompletionSource();
        return _tcs.Task;
    }

    /// <summary>
    /// Execute transition. Completes immediately if animation is disabled.
    /// </summary>
    internal void Execute()
    {
        _onStart?.Invoke();

        if (_animationDisabled)
        {
            Complete();
            return;
        }

        // Default to simple fade in/fade out animation
        // Can be overridden by subclasses or extended via AnimationPlayer
        var tween = _target.CreateTween();
        tween.TweenProperty(_target, "modulate:a", 1.0f, 0.3f)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(Callable.From(Complete));
    }

    private void Complete()
    {
        _onEnd?.Invoke();
        _tcs?.TrySetResult();
    }
}
