using System.Windows.Input;

namespace DotPudica.Core.Binding;

/// <summary>
/// Property binding core class. Connects ViewModel property (source) and View control property (target),
/// automatically synchronizes value changes on both ends according to BindingMode.
/// </summary>
public class PropertyBinding : IDisposable
{
    private readonly BindingPath _sourcePath;
    private readonly ITargetProxy _targetProxy;
    private readonly BindingMode _mode;
    private readonly IValueConverter? _converter;
    private object? _source;
    private bool _isUpdating; // Prevent circular updates
    private bool _disposed;

    /// <summary>
    /// Create property binding.
    /// </summary>
    /// <param name="targetProxy">Target control property proxy</param>
    /// <param name="sourcePath">Source property path (e.g., "Account.Username")</param>
    /// <param name="mode">Binding mode</param>
    /// <param name="converter">Optional value converter</param>
    public PropertyBinding(
        ITargetProxy targetProxy,
        string sourcePath,
        BindingMode mode = BindingMode.OneWay,
        IValueConverter? converter = null)
    {
        _targetProxy = targetProxy ?? throw new ArgumentNullException(nameof(targetProxy));
        _sourcePath = new BindingPath(sourcePath);
        _mode = mode;
        _converter = converter;

        // Listen to source path value changes
        _sourcePath.ValueChanged += OnSourceValueChanged;

        // In TwoWay or OneWayToSource mode, listen to target control value changes
        if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
        {
            _targetProxy.ValueChanged += OnTargetValueChanged;
        }
    }

    /// <summary>
    /// Bind to the specified source object (ViewModel) and perform initial synchronization.
    /// </summary>
    public void Bind(object? source)
    {
        _source = source;
        _sourcePath.Bind(source);
        UpdateTarget(); // Initial synchronization
    }

    /// <summary>
    /// Unbind.
    /// </summary>
    public void Unbind()
    {
        _sourcePath.Unbind();
        _source = null;
    }

    /// <summary>
    /// Source → Target: Update control when ViewModel property changes.
    /// </summary>
    private void UpdateTarget()
    {
        if (_mode == BindingMode.OneWayToSource)
            return;

        if (_isUpdating)
            return;

        _isUpdating = true;
        try
        {
            var value = _sourcePath.GetValue(_source);

            if (_converter != null)
            {
                value = _converter.Convert(value, _targetProxy.TargetType, null);
            }

            _targetProxy.SetValue(value);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// Target → Source: Update ViewModel when control value is modified by user.
    /// </summary>
    private void UpdateSource()
    {
        if (_mode is not (BindingMode.TwoWay or BindingMode.OneWayToSource))
            return;

        if (_isUpdating)
            return;

        _isUpdating = true;
        try
        {
            var value = _targetProxy.GetValue();

            if (_converter != null)
            {
                value = _converter.ConvertBack(value, _sourcePath.GetValueType(_source) ?? typeof(object), null);
            }

            _sourcePath.SetValue(_source, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnSourceValueChanged(object? sender, EventArgs e)
    {
        if (_mode == BindingMode.OneTime)
            return; // OneTime only synchronizes once during initial binding

        UpdateTarget();
    }

    private void OnTargetValueChanged(object? sender, EventArgs e)
    {
        UpdateSource();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _sourcePath.ValueChanged -= OnSourceValueChanged;
            if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
            {
                _targetProxy.ValueChanged -= OnTargetValueChanged;
            }
            _sourcePath.Dispose();
            _targetProxy.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Command binding. Binds ViewModel's ICommand to control events (e.g., Button.Pressed).
/// </summary>
public class CommandBinding : IDisposable
{
    private readonly BindingPath _commandPath;
    private readonly BindingPath? _parameterPath;
    private readonly Action _triggerSubscribe;
    private readonly Action _triggerUnsubscribe;
    private object? _source;
    private ICommand? _currentCommand;
    private bool _disposed;

    /// <summary>
    /// Create command binding.
    /// </summary>
    /// <param name="commandName">ICommand property name on ViewModel</param>
    /// <param name="parameterPath">Optional command parameter path</param>
    /// <param name="triggerSubscribe">Subscribe to control trigger event (executes command when called)</param>
    /// <param name="triggerUnsubscribe">Unsubscribe from control trigger event</param>
    public CommandBinding(
        string commandName,
        string? parameterPath,
        Action triggerSubscribe,
        Action triggerUnsubscribe)
    {
        _commandPath = new BindingPath(commandName);
        _parameterPath = parameterPath != null ? new BindingPath(parameterPath) : null;
        _triggerSubscribe = triggerSubscribe;
        _triggerUnsubscribe = triggerUnsubscribe;

        _commandPath.ValueChanged += OnCommandChanged;
    }

    /// <summary>
    /// Bind to source object.
    /// </summary>
    public void Bind(object? source)
    {
        Unbind();
        _source = source;
        _commandPath.Bind(source);
        _parameterPath?.Bind(source);

        _currentCommand = _commandPath.GetValue(source) as ICommand;
        if (_currentCommand != null)
        {
            _triggerSubscribe();
        }
    }

    /// <summary>
    /// Execute command (triggered by control event).
    /// </summary>
    public void Execute()
    {
        if (_currentCommand == null)
            return;

        var parameter = _parameterPath?.GetValue(_source);

        if (_currentCommand.CanExecute(parameter))
        {
            _currentCommand.Execute(parameter);
        }
    }

    /// <summary>
    /// Check if command can execute.
    /// </summary>
    public bool CanExecute()
    {
        if (_currentCommand == null)
            return false;

        var parameter = _parameterPath?.GetValue(_source);
        return _currentCommand.CanExecute(parameter);
    }

    public void Unbind()
    {
        _triggerUnsubscribe();
        _commandPath.Unbind();
        _parameterPath?.Unbind();
        _currentCommand = null;
        _source = null;
    }

    private void OnCommandChanged(object? sender, EventArgs e)
    {
        _triggerUnsubscribe();
        _currentCommand = _commandPath.GetValue(_source) as ICommand;
        if (_currentCommand != null)
        {
            _triggerSubscribe();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Unbind();
            _commandPath.ValueChanged -= OnCommandChanged;
            _commandPath.Dispose();
            _parameterPath?.Dispose();
            _disposed = true;
        }
    }
}
