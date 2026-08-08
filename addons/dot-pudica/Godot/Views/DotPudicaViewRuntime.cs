using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Binding.ControlProxies;
using Godot;

namespace DotPudica.Godot.Views;

/// <summary>
/// MVVM runtime host. Injected by the source generator into the View stub class,
/// responsible for managing the binding context and ViewModel lifecycle.
/// Binding declarations are provided as compile-time delegates, no expression trees, no reflection.
/// </summary>
public sealed class DotPudicaViewRuntime<TViewModel> : IDisposable
    where TViewModel : ObservableObject
{
    private readonly BindingContext _bindingContext = new();
    private IUiDispatcher _dispatcher = UiDispatcher.Immediate;
    private ViewModelLease<TViewModel> _lease = ViewModelLease<TViewModel>.External(null);
    private bool _uiContextCaptured;
    private bool _disposed;

    public BindingContext BindingContext => _bindingContext;

    /// <summary>
    /// Captures and validates the Godot UI synchronization context during _Ready.
    /// Bindings created afterwards marshal target updates back to this context. Idempotent for reuse.
    /// </summary>
    public void CaptureUiContext()
    {
        var context = Dispatcher.SynchronizationContext;
        if (!ReferenceEquals(SynchronizationContext.Current, context))
        {
            throw new InvalidOperationException(
                "DotPudica bindings must be initialized on the Godot main thread. Call DotPudicaInitialize from Node._Ready.");
        }

        if (_uiContextCaptured)
            return;

        _uiContextCaptured = true;
        _dispatcher = UiDispatcher.FromSynchronizationContext(context);
        _bindingContext.SetUiDispatcher(_dispatcher);
    }

    public TViewModel? ViewModel => _lease.ViewModel;

    /// <summary>
    /// Assigns a ViewModel and declares ownership.
    /// Use <see cref="ViewModelOwnership.Owned"/> when this view creates the instance;
    /// use <see cref="ViewModelOwnership.External"/> when sharing an instance owned elsewhere.
    /// </summary>
    public void SetViewModel(TViewModel? viewModel, ViewModelOwnership ownership)
    {
        VerifyUiAccess();
        if (ReferenceEquals(_lease.ViewModel, viewModel))
        {
            if (_lease.Ownership == ownership)
                return;

            _lease = new ViewModelLease<TViewModel>(viewModel, ownership);
            return;
        }

        _bindingContext.DataContext = null;
        _lease.Dispose();
        _lease = new ViewModelLease<TViewModel>(viewModel, ownership);
        _bindingContext.DataContext = viewModel;
    }

    public void BindProperty<TSourceValue, TTargetValue>(
        ITypedTargetProxy<TTargetValue> targetProxy,
        TypedBindingPath<TViewModel, TSourceValue> sourcePath,
        BindingMode mode,
        IValueConverter<TSourceValue, TTargetValue>? converter = null,
        Func<TSourceValue, TTargetValue>? mapForward = null,
        Func<TTargetValue, TSourceValue>? mapBack = null)
    {
        VerifyUiAccess();
        var binding = new PropertyBinding<TSourceValue, TTargetValue>(
            targetProxy, sourcePath, mode, converter, _dispatcher, mapForward, mapBack);
        _bindingContext.AddBinding(binding);
    }

    public void BindCommand(
        BaseButton target,
        string signal,
        TypedBindingPath<TViewModel, ICommand> commandPath,
        TypedBindingPath<TViewModel, object?>? parameterPath = null)
    {
        VerifyUiAccess();
        CommandBinding? commandBinding = null;
        Callable? callable = null;

        commandBinding = new CommandBinding(
            commandPath,
            parameterPath,
            triggerSubscribe: () =>
            {
                if (commandBinding is null)
                    return;
                callable = Callable.From(commandBinding.Execute);
                target.Connect(signal, callable.Value);
            },
            triggerUnsubscribe: () =>
            {
                if (callable is { } connected
                    && GodotObject.IsInstanceValid(target)
                    && target.IsConnected(signal, connected))
                {
                    target.Disconnect(signal, connected);
                }

                callable = null;
            },
            setCanExecute: canExecute => target.Disabled = !canExecute,
            dispatcher: _dispatcher);

        _bindingContext.AddBinding(commandBinding);
    }

    /// <summary>
    /// <paramref name="itemCommandGetter"/> is optional: injects an ICommand into each item view
    /// (via <see cref="IItemsControlItemCommand"/>), allowing item templates to bubble user interactions up to the ViewModel.
    /// </summary>
    public void BindItems<TCollection>(
        Container target,
        string itemScenePath,
        TypedBindingPath<TViewModel, TCollection> sourcePath,
        int poolSize = 0,
        Func<TViewModel, ICommand>? itemCommandGetter = null)
        where TCollection : class
    {
        VerifyUiAccess();
        var itemScene = ResourceLoader.Load<PackedScene>(itemScenePath)
            ?? throw new InvalidOperationException($"Failed to load PackedScene: {itemScenePath}");

        var proxy = new ContainerItemsProxy(target, itemScene, poolSize, BuildItemCommandProvider(itemCommandGetter));
        var binding = new CollectionBinding(proxy, sourcePath, _dispatcher);
        _bindingContext.AddBinding(binding);
    }

    /// <summary>
    /// <paramref name="itemCommandGetter"/> semantics match <see cref="BindItems{TCollection}"/>.
    /// </summary>
    public void BindVirtualizedItems<TCollection>(
        VirtualizedItemsControl target,
        string itemScenePath,
        TypedBindingPath<TViewModel, TCollection> sourcePath,
        Func<TViewModel, ICommand>? itemCommandGetter = null)
        where TCollection : class
    {
        VerifyUiAccess();
        var itemScene = ResourceLoader.Load<PackedScene>(itemScenePath)
            ?? throw new InvalidOperationException($"Failed to load PackedScene: {itemScenePath}");

        var proxy = new VirtualizedItemsProxy(target, itemScene, BuildItemCommandProvider(itemCommandGetter));
        var binding = new VirtualizedCollectionBinding(proxy, sourcePath, _dispatcher);
        _bindingContext.AddBinding(binding);
    }

    private Func<ICommand?>? BuildItemCommandProvider(Func<TViewModel, ICommand>? itemCommandGetter)
        => itemCommandGetter is null ? null : () => ViewModel is { } vm ? itemCommandGetter(vm) : null;

    /// <summary>
    /// Releases the view for pooling: clears bindings and releases the ViewModel lease —
    /// External drops the reference, Owned disposes the ViewModel. The node stays alive.
    /// </summary>
    public void Recycle()
    {
        VerifyUiAccess();
        _bindingContext.ClearBindings();
        _lease.Dispose();
        _lease = ViewModelLease<TViewModel>.External(null);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        VerifyUiAccess();
        _disposed = true;
        _bindingContext.Dispose();
        _lease.Dispose();
    }

    private void VerifyUiAccess()
    {
        if (!_dispatcher.CheckAccess())
            throw new InvalidOperationException("DotPudica View runtime must be accessed on the UI thread.");
    }
}
