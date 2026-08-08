using Godot;

namespace DotPudica.Godot.Views;

public class GodotTransition : ITransition
{
    private const float DefaultDuration = 0.3f;

    private readonly Control _target;
    private Action? _onStart;
    private Action? _onEnd;
    private bool _animationDisabled;
    private bool _completed;
    private float _fadeTarget = 1f;
    private TaskCompletionSource? _tcs;
    private Tween? _tween;

    public GodotTransition(Control target)
    {
        _target = target;
    }

    /// <summary>Target modulate alpha for the default fade tween (1 = fade in, 0 = fade out).</summary>
    internal GodotTransition FadeTo(float alpha)
    {
        _fadeTarget = alpha;
        return this;
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
        if (_completed)
            return Task.CompletedTask;

        _tcs ??= new TaskCompletionSource();
        return _tcs.Task;
    }

    /// <summary>
    /// Kill any running tween so Callable.From callbacks do not pin the ALC across unload.
    /// Does not invoke <see cref="OnEnd"/> — use <see cref="ForceComplete"/> to finish a transition.
    /// </summary>
    public void Cancel()
    {
        KillTween();
        if (_completed)
            return;

        _tcs?.TrySetCanceled();
        _tcs = null;
    }

    /// <summary>
    /// Abort the running tween (if any), snap to the fade target, and invoke completion callbacks.
    /// Only valid after <see cref="Execute"/> has started the transition.
    /// </summary>
    public void ForceComplete()
    {
        KillTween();
        ApplyAlpha(_fadeTarget);
        Complete();
    }

    internal void Execute()
    {
        _onStart?.Invoke();

        if (_animationDisabled)
        {
            ApplyAlpha(_fadeTarget);
            Complete();
            return;
        }

        // Fade-in starts from transparent so the tween is visible.
        if (_fadeTarget >= 1f)
            ApplyAlpha(0f);

        KillTween();

        _tween = _target.CreateTween();
        _tween.TweenProperty(_target, "modulate:a", _fadeTarget, DefaultDuration)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.InOut);
        _tween.TweenCallback(Callable.From(Complete));
    }

    private void ApplyAlpha(float alpha)
    {
        if (!GodotObject.IsInstanceValid(_target))
            return;

        var modulate = _target.Modulate;
        modulate.A = alpha;
        _target.Modulate = modulate;
    }

    private void KillTween()
    {
        if (_tween is not null && GodotObject.IsInstanceValid(_tween))
            _tween.Kill();
        _tween = null;
    }

    private void Complete()
    {
        if (_completed)
            return;

        _completed = true;
        _tween = null;
        _onEnd?.Invoke();
        _tcs?.TrySetResult();
    }
}

/// <summary>
/// Already-finished transition for cases like QueuedPopup enqueue (accepted into queue, not shown yet).
/// Does not touch any <see cref="Control"/>. Late <see cref="OnStart"/>/<see cref="OnEnd"/>
/// callbacks run synchronously (enqueue completion ≠ window displayed).
/// </summary>
internal sealed class CompletedTransition : ITransition
{
    public static CompletedTransition Instance { get; } = new();

    public ITransition OnStart(Action callback)
    {
        callback();
        return this;
    }

    public ITransition OnEnd(Action callback)
    {
        callback();
        return this;
    }

    public ITransition DisableAnimation(bool disabled) => this;

    public Task WaitForFinish() => Task.CompletedTask;

    public void Cancel() { }
}
