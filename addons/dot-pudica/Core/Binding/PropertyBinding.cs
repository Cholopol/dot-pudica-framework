using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DotPudica.Core.Binding;

/// <summary>
/// Dispatcher, version token, and frame coalescing. Hot-path read/compare/write lives in the generic subclass.
/// </summary>
public abstract class PropertyBindingBase : IDisposable, IBinding
{
    private readonly IUiDispatcher _dispatcher;
    private readonly Action _runTargetUpdate;
    private readonly UiDispatchCoalescer _coalescer = new();
    private readonly UiDispatchCoalescer.Channel _targetUpdateChannel;
    private readonly BindingMode _mode;
    private bool _disposed;
    private int _targetUpdateForce;

    protected PropertyBindingBase(IUiDispatcher? dispatcher, BindingMode mode = BindingMode.OneWay)
    {
        _dispatcher = dispatcher ?? UiDispatcher.Immediate;
        _mode = mode;
        _targetUpdateChannel = _coalescer.CreateChannel();
        _runTargetUpdate = RunTargetUpdate;
    }

    protected IUiDispatcher Dispatcher => _dispatcher;

    protected bool IsDisposed => _disposed;

    public void Bind(object? source)
    {
        VerifyUiAccess();
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
        _coalescer.AdvanceVersion();
        BindCore(source);
        UpdateTarget(force: true);
    }

    public void Unbind()
    {
        VerifyUiAccess();
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
        _coalescer.AdvanceVersion();
        UnbindCore();
    }

    protected abstract void BindCore(object? source);

    protected abstract void UnbindCore();

    protected abstract void SubscribeSourceChanged();

    protected abstract void UnsubscribeSourceChanged();

    protected virtual void SubscribeTargetChanged()
    {
        if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
            SubscribeTargetChangedCore();
    }

    protected virtual void UnsubscribeTargetChanged()
    {
        if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
            UnsubscribeTargetChangedCore();
    }

    protected abstract void SubscribeTargetChangedCore();

    protected abstract void UnsubscribeTargetChangedCore();

    protected abstract void RunTargetUpdateCore(bool force);

    protected abstract void UpdateSourceCore();

    protected void OnSourceValueChanged(object? sender, EventArgs e)
    {
        if (ShouldIgnoreSourceChange())
            return;
        UpdateTarget();
    }

    protected void OnTargetValueChanged(object? sender, EventArgs e) => UpdateSource();

    protected virtual bool ShouldIgnoreSourceChange() => _mode == BindingMode.OneTime;

    private void UpdateTarget(bool force = false)
    {
        if (IsOneWayToSource())
            return;

        _targetUpdateChannel.Stamp(_coalescer.CurrentVersion);
        if (force)
            Interlocked.Exchange(ref _targetUpdateForce, 1);

        ScheduleTargetUpdate();
    }

    private void UpdateSource()
    {
        VerifyUiAccess();
        if (!SupportsSourceUpdate())
            return;

        UpdateSourceCore();
    }

    protected virtual bool IsOneWayToSource() => _mode == BindingMode.OneWayToSource;

    protected virtual bool SupportsSourceUpdate() =>
        _mode is BindingMode.TwoWay or BindingMode.OneWayToSource;

    private void ScheduleTargetUpdate()
    {
        if (_dispatcher.CheckAccess())
        {
            RunTargetUpdate();
            return;
        }

        _targetUpdateChannel.TryMarkQueued(() => _dispatcher.Post(_runTargetUpdate));
    }

    private void RunTargetUpdate()
    {
        _targetUpdateChannel.ClearScheduled();

        var version = _targetUpdateChannel.ReadVersion();
        var force = Interlocked.Exchange(ref _targetUpdateForce, 0) != 0;
        if (_disposed || version != _coalescer.CurrentVersion)
            return;

        RunTargetUpdateCore(force);
    }

    public void Dispose()
    {
        VerifyUiAccess();
        if (_disposed)
            return;

        UnsubscribeSourceChanged();
        UnsubscribeTargetChanged();
        DisposeCore();
        _disposed = true;
    }

    protected abstract void DisposeCore();

    protected void VerifyUiAccess()
    {
        if (!_dispatcher.CheckAccess())
            throw new InvalidOperationException("Target-side binding operations must be executed on the UI thread.");
    }
}

/// <summary>
/// Generic source/target pipeline; <see cref="EqualityComparer{T}.Default"/> compares with no boxing on the value-type hot path.
/// </summary>
public class PropertyBinding<TSourceValue, TTargetValue> : PropertyBindingBase
{
    private readonly IBindingPath<TSourceValue> _sourcePath;
    private readonly ITypedTargetProxy<TTargetValue> _targetProxy;
    private readonly IValueConverter<TSourceValue, TTargetValue>? _converter;
    private readonly Func<TSourceValue, TTargetValue>? _mapForward;
    private readonly Func<TTargetValue, TSourceValue>? _mapBack;

    public PropertyBinding(
        ITypedTargetProxy<TTargetValue> targetProxy,
        IBindingPath<TSourceValue> sourcePath,
        BindingMode mode = BindingMode.OneWay,
        IValueConverter<TSourceValue, TTargetValue>? converter = null,
        IUiDispatcher? dispatcher = null,
        Func<TSourceValue, TTargetValue>? mapForward = null,
        Func<TTargetValue, TSourceValue>? mapBack = null)
        : base(dispatcher, mode)
    {
        _targetProxy = targetProxy;
        _sourcePath = sourcePath;
        _converter = converter;
        _mapForward = mapForward;
        _mapBack = mapBack;

        SubscribeSourceChanged();
        SubscribeTargetChanged();
    }

    protected override void BindCore(object? source) => _sourcePath.Bind(source);

    protected override void UnbindCore() => _sourcePath.Unbind();

    protected override void SubscribeSourceChanged() => _sourcePath.ValueChanged += OnSourceValueChanged;

    protected override void UnsubscribeSourceChanged() => _sourcePath.ValueChanged -= OnSourceValueChanged;

    protected override void SubscribeTargetChangedCore() => _targetProxy.ValueChanged += OnTargetValueChanged;

    protected override void UnsubscribeTargetChangedCore() => _targetProxy.ValueChanged -= OnTargetValueChanged;

    protected override void RunTargetUpdateCore(bool force)
    {
        var sourceValue = _sourcePath.GetValue();
        var targetValue = MapToTarget(sourceValue);

        if (!force && EqualityComparer<TTargetValue>.Default.Equals(_targetProxy.GetValue(), targetValue))
            return;

        _targetProxy.SetValue(targetValue);
    }

    protected override void UpdateSourceCore()
    {
        var targetValue = _targetProxy.GetValue();
        var sourceValue = MapToSource(targetValue);

        if (EqualityComparer<TSourceValue>.Default.Equals(_sourcePath.GetValue(), sourceValue))
            return;

        _sourcePath.SetValue(sourceValue);
    }

    protected override void DisposeCore()
    {
        _sourcePath.Dispose();
        _targetProxy.Dispose();
    }

    private TTargetValue MapToTarget(TSourceValue sourceValue)
    {
        if (_converter is not null)
            return _converter.Convert(sourceValue);
        if (_mapForward is not null)
            return _mapForward(sourceValue);
        if (typeof(TSourceValue) == typeof(TTargetValue))
            return Unsafe.As<TSourceValue, TTargetValue>(ref sourceValue);
        throw new InvalidOperationException(
            $"Cannot map {typeof(TSourceValue)} to {typeof(TTargetValue)}: provide a converter or mapForward.");
    }

    private TSourceValue MapToSource(TTargetValue targetValue)
    {
        if (_converter is not null)
            return _converter.ConvertBack(targetValue);
        if (_mapBack is not null)
            return _mapBack(targetValue);
        if (typeof(TTargetValue) == typeof(TSourceValue))
            return Unsafe.As<TTargetValue, TSourceValue>(ref targetValue);
        throw new InvalidOperationException(
            $"Cannot map {typeof(TTargetValue)} to {typeof(TSourceValue)}: provide a converter or mapBack.");
    }
}

/// <summary>
/// Type-erased object pipeline (tests/compat). Prefer <see cref="PropertyBinding{TSourceValue,TTargetValue}"/> in production.
/// </summary>
public sealed class PropertyBinding : PropertyBindingBase
{
    private readonly IBindingPath _sourcePath;
    private readonly ITargetProxy _targetProxy;
    private readonly IValueConverter? _converter;

    public PropertyBinding(
        ITargetProxy targetProxy,
        IBindingPath sourcePath,
        BindingMode mode = BindingMode.OneWay,
        IValueConverter? converter = null,
        IUiDispatcher? dispatcher = null)
        : base(dispatcher, mode)
    {
        _targetProxy = targetProxy;
        _sourcePath = sourcePath;
        _converter = converter;

        SubscribeSourceChanged();
        SubscribeTargetChanged();
    }

    protected override void BindCore(object? source) => _sourcePath.Bind(source);

    protected override void UnbindCore() => _sourcePath.Unbind();

    protected override void SubscribeSourceChanged() => _sourcePath.ValueChanged += OnSourceValueChanged;

    protected override void UnsubscribeSourceChanged() => _sourcePath.ValueChanged -= OnSourceValueChanged;

    protected override void SubscribeTargetChangedCore() => _targetProxy.ValueChanged += OnTargetValueChanged;

    protected override void UnsubscribeTargetChangedCore() => _targetProxy.ValueChanged -= OnTargetValueChanged;

    protected override void RunTargetUpdateCore(bool force)
    {
        var value = _sourcePath.GetValue();
        if (_converter is not null)
            value = _converter.Convert(value, typeof(object));

        if (!force && Equals(_targetProxy.GetValue(), value))
            return;

        _targetProxy.SetValue(value);
    }

    protected override void UpdateSourceCore()
    {
        var value = _targetProxy.GetValue();
        if (_converter is not null)
            value = _converter.ConvertBack(value, typeof(object));

        if (Equals(_sourcePath.GetValue(), value))
            return;

        _sourcePath.SetValue(value);
    }

    protected override void DisposeCore()
    {
        _sourcePath.Dispose();
        _targetProxy.Dispose();
    }
}

public class CommandBinding : IDisposable, IBinding
{
    private readonly IBindingPath _commandPath;
    private readonly IBindingPath? _parameterPath;
    private readonly Action _triggerSubscribe;
    private readonly Action _triggerUnsubscribe;
    private readonly Action<bool>? _setCanExecute;
    private readonly IUiDispatcher _dispatcher;
    private readonly Action _runCommandReplacement;
    private readonly Action _runCanExecuteUpdate;
    private object? _source;
    private ICommand? _currentCommand;
    private bool _disposed;
    private readonly UiDispatchCoalescer _coalescer = new();
    private readonly UiDispatchCoalescer.Channel _commandReplacementChannel;
    private readonly UiDispatchCoalescer.Channel _canExecuteUpdateChannel;

    public CommandBinding(
        IBindingPath commandPath,
        IBindingPath? parameterPath,
        Action triggerSubscribe,
        Action triggerUnsubscribe,
        Action<bool>? setCanExecute = null,
        IUiDispatcher? dispatcher = null)
    {
        _commandPath = commandPath;
        _parameterPath = parameterPath;
        _triggerSubscribe = triggerSubscribe;
        _triggerUnsubscribe = triggerUnsubscribe;
        _setCanExecute = setCanExecute;
        _dispatcher = dispatcher ?? UiDispatcher.Immediate;
        _commandReplacementChannel = _coalescer.CreateChannel();
        _canExecuteUpdateChannel = _coalescer.CreateChannel();
        _runCommandReplacement = RunCommandReplacement;
        _runCanExecuteUpdate = RunCanExecuteUpdate;

        _commandPath.ValueChanged += OnCommandChanged;
        if (_parameterPath is not null)
            _parameterPath.ValueChanged += OnParameterChanged;
    }

    public void Bind(object? source)
    {
        VerifyUiAccess();
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
        Unbind(updateCanExecute: false);
        _source = source;
        _commandPath.Bind(source);
        _parameterPath?.Bind(source);

        ReplaceCommand(_commandPath.GetValue() as ICommand);
    }

    public void Execute()
    {
        VerifyUiAccess();
        if (_currentCommand is null)
            return;

        var parameter = _parameterPath?.GetValue();
        if (_currentCommand.CanExecute(parameter))
            _currentCommand.Execute(parameter);
    }

    public bool CanExecute()
    {
        if (_currentCommand is null)
            return false;

        var parameter = _parameterPath?.GetValue();
        return _currentCommand.CanExecute(parameter);
    }

    public void Unbind()
    {
        VerifyUiAccess();
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
        Unbind(updateCanExecute: true);
    }

    private void Unbind(bool updateCanExecute)
    {
        _coalescer.AdvanceVersion();
        ReplaceCommand(null, updateCanExecute);
        _commandPath.Unbind();
        _parameterPath?.Unbind();
        _source = null;
    }

    private void OnCommandChanged(object? sender, EventArgs e)
    {
        _commandReplacementChannel.Stamp(_coalescer.CurrentVersion);
        _commandReplacementChannel.TryMarkQueued(() => _dispatcher.Post(_runCommandReplacement));
    }

    private void OnParameterChanged(object? sender, EventArgs e) => UpdateCanExecute();

    private void ReplaceCommand(ICommand? command, bool updateCanExecute = true)
    {
        _triggerUnsubscribe();
        if (_currentCommand is not null)
            _currentCommand.CanExecuteChanged -= OnCanExecuteChanged;

        _currentCommand = command;
        if (_currentCommand is not null)
        {
            _currentCommand.CanExecuteChanged += OnCanExecuteChanged;
            _triggerSubscribe();
        }

        if (updateCanExecute)
            UpdateCanExecute();
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e) => UpdateCanExecute();

    private void UpdateCanExecute()
    {
        if (_setCanExecute is null)
            return;

        _canExecuteUpdateChannel.Stamp(_coalescer.CurrentVersion);
        _canExecuteUpdateChannel.TryMarkQueued(() => _dispatcher.Post(_runCanExecuteUpdate));
    }

    private void RunCommandReplacement()
    {
        _commandReplacementChannel.ClearScheduled();
        if (_disposed || _commandReplacementChannel.ReadVersion() != _coalescer.CurrentVersion)
            return;

        ReplaceCommand(_commandPath.GetValue() as ICommand);
    }

    private void RunCanExecuteUpdate()
    {
        _canExecuteUpdateChannel.ClearScheduled();
        if (_disposed || _canExecuteUpdateChannel.ReadVersion() != _coalescer.CurrentVersion)
            return;

        _setCanExecute!(CanExecute());
    }

    public void Dispose()
    {
        VerifyUiAccess();
        if (_disposed)
            return;

        Unbind();
        _commandPath.ValueChanged -= OnCommandChanged;
        if (_parameterPath is not null)
            _parameterPath.ValueChanged -= OnParameterChanged;
        _commandPath.Dispose();
        _parameterPath?.Dispose();
        _disposed = true;
    }

    private void VerifyUiAccess()
    {
        if (!_dispatcher.CheckAccess())
            throw new InvalidOperationException("Command binding lifecycle and execution must be on the UI thread.");
    }
}
