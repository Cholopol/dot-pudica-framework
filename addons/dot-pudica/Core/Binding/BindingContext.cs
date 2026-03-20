namespace DotPudica.Core.Binding;

/// <summary>
/// Binding context. Holds DataContext (ViewModel) reference and manages all bindings belonging to this context.
/// Automatically disconnects old bindings and re-establishes new bindings when DataContext is switched.
/// </summary>
public class BindingContext : IDisposable
{
    private readonly List<PropertyBinding> _propertyBindings = new();
    private readonly List<CommandBinding> _commandBindings = new();
    private object? _dataContext;
    private bool _disposed;

    /// <summary>
    /// DataContext changed event.
    /// </summary>
    public event EventHandler? DataContextChanged;

    /// <summary>
    /// Current data context (usually ViewModel).
    /// When set to a new value, automatically rebinds all registered bindings.
    /// </summary>
    public object? DataContext
    {
        get => _dataContext;
        set
        {
            if (ReferenceEquals(_dataContext, value))
                return;

            _dataContext = value;
            RebindAll();
            DataContextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Add property binding. Binds immediately if DataContext already exists.
    /// </summary>
    public void AddBinding(PropertyBinding binding)
    {
        _propertyBindings.Add(binding);
        if (_dataContext != null)
        {
            binding.Bind(_dataContext);
        }
    }

    /// <summary>
    /// Add command binding. Binds immediately if DataContext already exists.
    /// </summary>
    public void AddBinding(CommandBinding binding)
    {
        _commandBindings.Add(binding);
        if (_dataContext != null)
        {
            binding.Bind(_dataContext);
        }
    }

    /// <summary>
    /// Remove and dispose the specified property binding.
    /// </summary>
    public void RemoveBinding(PropertyBinding binding)
    {
        if (_propertyBindings.Remove(binding))
        {
            binding.Dispose();
        }
    }

    /// <summary>
    /// Remove and dispose the specified command binding.
    /// </summary>
    public void RemoveBinding(CommandBinding binding)
    {
        if (_commandBindings.Remove(binding))
        {
            binding.Dispose();
        }
    }

    /// <summary>
    /// Clear all bindings.
    /// </summary>
    public void ClearBindings()
    {
        foreach (var binding in _propertyBindings)
            binding.Dispose();
        _propertyBindings.Clear();

        foreach (var binding in _commandBindings)
            binding.Dispose();
        _commandBindings.Clear();
    }

    /// <summary>
    /// Disconnect old DataContext and rebind all registered bindings to the new DataContext.
    /// </summary>
    private void RebindAll()
    {
        foreach (var binding in _propertyBindings)
        {
            binding.Unbind();
            if (_dataContext != null)
                binding.Bind(_dataContext);
        }

        foreach (var binding in _commandBindings)
        {
            binding.Unbind();
            if (_dataContext != null)
                binding.Bind(_dataContext);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            ClearBindings();
            _disposed = true;
        }
    }
}
