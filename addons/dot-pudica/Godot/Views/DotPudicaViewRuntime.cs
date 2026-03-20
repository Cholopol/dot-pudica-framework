using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding;
using Godot;

namespace DotPudica.Godot.Views;

/// <summary>
/// Runtime MVVM host injected into Godot script stubs by the DotPudica source generator.
/// This type is not a Godot script itself; it only manages binding state for a script instance.
/// </summary>
public sealed class DotPudicaViewRuntime<TViewModel> : IDisposable
    where TViewModel : ObservableObject
{
    private readonly BindingContext _bindingContext = new();
    private TViewModel? _viewModel;

    public BindingContext BindingContext => _bindingContext;

    public TViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            _bindingContext.DataContext = value;
        }
    }

    public void BindProperty(
        Control target,
        string targetProperty,
        string? changeSignal,
        string viewModelPath,
        BindingMode mode,
        IValueConverter? converter = null)
    {
        var proxy = GodotTargetProxyFactory.Create(target, targetProperty, changeSignal);
        var binding = new PropertyBinding(proxy, viewModelPath, mode, converter);
        _bindingContext.AddBinding(binding);
    }

    public void BindCommand(
        BaseButton target,
        string signal,
        string commandName,
        string? parameterPath)
    {
        Callable? callable = null;
        CommandBinding? commandBinding = null;

        commandBinding = new CommandBinding(
            commandName,
            parameterPath,
            triggerSubscribe: () =>
            {
                var binding = commandBinding;
                if (binding is null)
                    return;

                callable = Callable.From(binding.Execute);
                target.Connect(signal, callable.Value);
            },
            triggerUnsubscribe: () =>
            {
                if (callable.HasValue && target.IsConnected(signal, callable.Value))
                {
                    target.Disconnect(signal, callable.Value);
                }
            });

        _bindingContext.AddBinding(commandBinding);
    }

    public void Dispose()
    {
        _bindingContext.Dispose();
    }
}
