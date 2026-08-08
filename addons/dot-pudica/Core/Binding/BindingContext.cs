using System.Threading;

namespace DotPudica.Core.Binding;

/// <summary>
/// Schedules target-side UI work. Core bindings depend only on this small contract,
/// allowing host adapters to keep UI objects on their required thread.
/// </summary>
public interface IUiDispatcher
{
    bool CheckAccess();
    void Post(Action action);
}

/// <summary>Shared dispatchers for headless tests and UI hosts with a synchronization context.</summary>
public static class UiDispatcher
{
    public static IUiDispatcher Immediate { get; } = new ImmediateUiDispatcher();

    public static IUiDispatcher FromSynchronizationContext(SynchronizationContext context)
        => new SynchronizationContextUiDispatcher(context);

    public static IUiDispatcher CaptureCurrentOrImmediate()
        => SynchronizationContext.Current is { } context
            ? FromSynchronizationContext(context)
            : Immediate;

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public void Post(Action action) => action();
    }

    private sealed class SynchronizationContextUiDispatcher(SynchronizationContext context) : IUiDispatcher
    {
        public bool CheckAccess() => ReferenceEquals(SynchronizationContext.Current, context);

        public void Post(Action action)
        {
            if (CheckAccess())
            {
                action();
                return;
            }

            context.Post(static state => ((Action)state!).Invoke(), action);
        }
    }
}

/// <summary>
/// Owns bindings for one ViewModel. Setting <see cref="DataContext"/> rebinds all registered bindings.
/// </summary>
public class BindingContext : IDisposable
{
    private readonly List<IBinding> _bindings = new();
    private IUiDispatcher _dispatcher = UiDispatcher.Immediate;
    private object? _dataContext;
    private bool _disposed;

    public event EventHandler? DataContextChanged;

    /// <summary>Must be set before any bindings are created.</summary>
    public void SetUiDispatcher(IUiDispatcher dispatcher)
    {
        if (_bindings.Count != 0)
            throw new InvalidOperationException("The UI dispatcher must be set before creating bindings.");

        _dispatcher = dispatcher;
    }

    public object? DataContext
    {
        get => _dataContext;
        set
        {
            VerifyUiAccess();

            if (ReferenceEquals(_dataContext, value))
                return;

            _dataContext = value;
            RebindAll();
            DataContextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void AddBinding(PropertyBindingBase binding) => AddCore(binding);
    public void AddBinding(CommandBinding binding) => AddCore(binding);
    public void AddBinding(CollectionBinding binding) => AddCore(binding);
    public void AddBinding(VirtualizedCollectionBinding binding) => AddCore(binding);

    private void AddCore(IBinding binding)
    {
        VerifyUiAccess();
        _bindings.Add(binding);
        if (_dataContext != null)
            binding.Bind(_dataContext);
    }

    public void RemoveBinding(PropertyBindingBase binding) => RemoveCore(binding);
    public void RemoveBinding(CommandBinding binding) => RemoveCore(binding);
    public void RemoveBinding(CollectionBinding binding) => RemoveCore(binding);
    public void RemoveBinding(VirtualizedCollectionBinding binding) => RemoveCore(binding);

    private void RemoveCore(IBinding binding)
    {
        VerifyUiAccess();
        if (_bindings.Remove(binding))
            binding.Dispose();
    }

    public void ClearBindings()
    {
        VerifyUiAccess();
        foreach (var binding in _bindings)
            binding.Dispose();
        _bindings.Clear();
    }

    private void RebindAll()
    {
        foreach (var binding in _bindings)
        {
            binding.Unbind();
            if (_dataContext != null)
                binding.Bind(_dataContext);
        }
    }

    public void Dispose()
    {
        VerifyUiAccess();
        if (!_disposed)
        {
            ClearBindings();
            _dataContext = null;
            _disposed = true;
        }
    }

    private void VerifyUiAccess()
    {
        if (!_dispatcher.CheckAccess())
            throw new InvalidOperationException("Binding context lifecycle operations must be executed on the UI thread.");
    }
}
