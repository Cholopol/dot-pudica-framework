using System.Windows.Input;
using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// CommandBinding unit tests. Verifies command execution, parameter resolution, and lifecycle.
/// </summary>
public class CommandBindingTests
{
    private static TypedBindingPath<CommandViewModel, ICommand?> CommandPath()
        => BindingPathFactory.Create(
            static (CommandViewModel vm) => vm.Command,
            static (vm, v) => vm.Command = v,
            "Command");

    private static TypedBindingPath<CommandViewModel, string> ParameterPath()
        => BindingPathFactory.Create(
            static (CommandViewModel vm) => vm.Parameter,
            static (vm, v) => vm.Parameter = v,
            "Parameter");

    [Fact]
    public void Execute_WithValidCommand_CallsCommand()
    {
        var vm = new CommandViewModel();
        vm.Command = vm.DefaultCommand;
        var path = CommandPath();

        var subscribed = false;
        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => subscribed = true,
            triggerUnsubscribe: () => subscribed = false);

        binding.Bind(vm);
        binding.Execute();

        Assert.True(subscribed);
        Assert.Equal(1, vm.ExecuteCount);
    }

    [Fact]
    public void Execute_WithNullCommand_DoesNothing()
    {
        var vm = new CommandViewModel { Command = null };
        var path = CommandPath();

        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => { });

        binding.Bind(vm);
        binding.Execute();

        Assert.Equal(0, vm.ExecuteCount);
    }

    [Fact]
    public void Execute_OutsideUiThread_Throws()
    {
        var vm = new CommandViewModel { Command = null };
        var dispatcher = new QueuedUiDispatcher();
        var path = CommandPath();
        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => { },
            dispatcher: dispatcher);

        binding.Bind(vm);
        dispatcher.HasAccess = false;

        Assert.Throws<InvalidOperationException>(binding.Execute);
    }

    [Fact]
    public void CanExecute_WithValidCommand_ReturnsTrue()
    {
        var vm = new CommandViewModel();
        vm.Command = vm.DefaultCommand;
        var path = CommandPath();

        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => { });

        binding.Bind(vm);

        Assert.True(binding.CanExecute());
    }

    [Fact]
    public void CanExecute_WithNullCommand_ReturnsFalse()
    {
        var vm = new CommandViewModel { Command = null };
        var path = CommandPath();

        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => { });

        binding.Bind(vm);

        Assert.False(binding.CanExecute());
    }

    [Fact]
    public void CanExecuteChanged_UpdatesTargetState()
    {
        var canExecute = false;
        var command = new RelayCommand(() => { }, () => canExecute);
        var vm = new CommandViewModel { Command = command };
        var states = new List<bool>();
        var path = CommandPath();
        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => { },
            setCanExecute: states.Add);

        binding.Bind(vm);
        canExecute = true;
        command.NotifyCanExecuteChanged();

        Assert.Equal(new[] { false, true }, states);
    }

    [Fact]
    public void ParameterChange_UpdatesTargetState()
    {
        var vm = new CommandViewModel { Parameter = "blocked" };
        vm.Command = new RelayCommand<object?>(_ => { }, parameter => Equals(parameter, "allowed"));
        var states = new List<bool>();
        var binding = new CommandBinding(
            CommandPath(),
            ParameterPath(),
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => { },
            setCanExecute: states.Add);

        binding.Bind(vm);
        vm.Parameter = "allowed";

        Assert.Equal(new[] { false, true }, states);
    }

    [Fact]
    public void Bind_ReplacingCommand_Resubscribes()
    {
        var vm = new CommandViewModel { Command = null };
        var path = CommandPath();

        var subscribeCount = 0;
        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => subscribeCount++,
            triggerUnsubscribe: () => { });

        binding.Bind(vm);
        Assert.Equal(0, subscribeCount);

        vm.Command = vm.DefaultCommand;
        Assert.Equal(1, subscribeCount);
    }

    [Fact]
    public void CommandChange_DispatchesTriggerSubscription()
    {
        var vm = new CommandViewModel { Command = null };
        var dispatcher = new QueuedUiDispatcher();
        var path = CommandPath();
        var subscribeCount = 0;
        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => subscribeCount++,
            triggerUnsubscribe: () => { },
            dispatcher: dispatcher);

        binding.Bind(vm);
        vm.Command = vm.DefaultCommand;

        Assert.Equal(0, subscribeCount);
        dispatcher.RunAll();
        Assert.Equal(1, subscribeCount);
    }

    [Fact]
    public void Unbind_StopsReceivingNotifications()
    {
        var vm = new CommandViewModel { Command = null };
        var path = CommandPath();

        var subscribeCount = 0;
        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => subscribeCount++,
            triggerUnsubscribe: () => { });

        binding.Bind(vm);
        binding.Unbind();

        var countBefore = subscribeCount;
        vm.Command = vm.DefaultCommand;

        Assert.Equal(countBefore, subscribeCount);
    }

    [Fact]
    public void Execute_WithParameter_PassesParameterToCommand()
    {
        var vm = new CommandViewModel { Parameter = "MyParam" };
        var receivedParam = (object?)null;

        var command = new RelayCommand<object?>(p =>
        {
            receivedParam = p;
            vm.ExecuteCount++;
        });

        vm.Command = command;

        var binding = new CommandBinding(
            CommandPath(),
            ParameterPath(),
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => { });

        binding.Bind(vm);
        binding.Execute();

        Assert.Equal("MyParam", receivedParam);
        Assert.Equal(1, vm.ExecuteCount);
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        var vm = new CommandViewModel();
        vm.Command = vm.DefaultCommand;
        var path = CommandPath();

        var disposed = false;
        var binding = new CommandBinding(
            path,
            parameterPath: null,
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => disposed = true);

        binding.Bind(vm);
        binding.Dispose();

        Assert.True(disposed);
    }
}
