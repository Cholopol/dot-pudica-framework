using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// PropertyBinding unit tests. Verifies value synchronization behavior for all binding modes.
/// </summary>
public class PropertyBindingTests
{
    private static TypedBindingPath<SimpleViewModel, string> NamePath()
        => BindingPathFactory.Create(
            static (SimpleViewModel vm) => vm.Name,
            static (vm, v) => vm.Name = v,
            "Name");

    [Fact]
    public void OneWay_SourceChange_UpdatesTarget()
    {
        var vm = new SimpleViewModel { Name = "Init" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);

        binding.Bind(vm);
        vm.Name = "Updated";

        Assert.Equal("Updated", proxy.SetValues.Last());
    }

    [Fact]
    public void OneWay_InitialBind_SyncsTarget()
    {
        var vm = new SimpleViewModel { Name = "Initial" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);

        binding.Bind(vm);

        Assert.Equal("Initial", proxy.SetValues.Last());
    }

    [Fact]
    public void OneWay_Dispatcher_DefersWorkerTargetUpdate()
    {
        var vm = new SimpleViewModel { Name = "Initial" };
        var proxy = new StubTargetProxy<string>();
        var dispatcher = new QueuedUiDispatcher();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay, dispatcher: dispatcher);

        binding.Bind(vm);
        Assert.Equal("Initial", proxy.SetValues.Last());

        dispatcher.HasAccess = false;
        vm.Name = "Updated";

        Assert.Equal(1, proxy.SetValueCallCount);
        dispatcher.RunAll();
        Assert.Equal("Updated", proxy.SetValues.Last());
    }

    [Fact]
    public void OneWay_Dispatcher_DropsQueuedUpdateAfterUnbind()
    {
        var vm = new SimpleViewModel { Name = "Initial" };
        var proxy = new StubTargetProxy<string>();
        var dispatcher = new QueuedUiDispatcher();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay, dispatcher: dispatcher);

        binding.Bind(vm);
        binding.Unbind();
        dispatcher.RunAll();

        Assert.Equal(1, proxy.SetValueCallCount);
    }

    [Fact]
    public void TwoWay_SourceChange_UpdatesTarget()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.TwoWay);

        binding.Bind(vm);
        vm.Name = "New";

        Assert.Equal("New", proxy.SetValues.Last());
    }

    [Fact]
    public void TwoWay_TargetChange_UpdatesSource()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var proxy = new StubTargetProxy<string>("Old");
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.TwoWay);

        binding.Bind(vm);
        proxy.SimulateUserInput("FromView");

        Assert.Equal("FromView", vm.Name);
    }

    [Fact]
    public void TwoWay_TargetChange_OutsideUiThread_Throws()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var proxy = new StubTargetProxy<string>("Old");
        var dispatcher = new QueuedUiDispatcher();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.TwoWay, dispatcher: dispatcher);

        binding.Bind(vm);
        dispatcher.HasAccess = false;

        Assert.Throws<InvalidOperationException>(() => proxy.SimulateUserInput("FromView"));
    }

    [Fact]
    public void OneTime_OnlySyncsOnce()
    {
        var vm = new SimpleViewModel { Name = "Initial" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneTime);

        binding.Bind(vm);
        Assert.Equal("Initial", proxy.SetValues.Last());

        vm.Name = "Changed";
        Assert.Equal("Initial", proxy.SetValues.Last());
    }

    [Fact]
    public void OneWayToSource_TargetChange_UpdatesSource()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var proxy = new StubTargetProxy<string>("FromView");
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWayToSource);

        binding.Bind(vm);
        proxy.SimulateUserInput("FromView");

        Assert.Equal("FromView", vm.Name);
    }

    [Fact]
    public void OneWayToSource_SourceChange_DoesNotUpdateTarget()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWayToSource);

        binding.Bind(vm);
        Assert.Equal(0, proxy.SetValueCallCount);

        vm.Name = "New";
        Assert.Equal(0, proxy.SetValueCallCount);
    }

    [Fact]
    public void TwoWay_PreventsCircularUpdate()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var proxy = new StubTargetProxy<string>("Old");
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.TwoWay);

        binding.Bind(vm);
        // Initial sync triggers one SetValue
        var initialCount = proxy.SetValueCallCount;

        // Simulate user input: target → source → (potential circular) target.
        // Source updated value is already the same as target, so it should not write back to target.
        proxy.SimulateUserInput("New");

        Assert.Equal(initialCount, proxy.SetValueCallCount);
        Assert.Equal("New", vm.Name);
    }

    [Fact]
    public void Converter_ConvertAppliesOnOneWay()
    {
        var vm = new SimpleViewModel { Name = "hello" };
        var proxy = new StubTargetProxy<string>();
        var converter = new ReverseStringConverter();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay, converter);

        binding.Bind(vm);

        Assert.Equal("olleh", proxy.SetValues.Last());
    }

    [Fact]
    public void Converter_ConvertBackAppliesOnTwoWay()
    {
        var vm = new SimpleViewModel { Name = "hello" };
        var proxy = new StubTargetProxy<string>("hello");
        var converter = new ReverseStringConverter();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.TwoWay, converter);

        binding.Bind(vm);
        proxy.SimulateUserInput("dlrow");

        // ConvertBack("dlrow") = "world"
        Assert.Equal("world", vm.Name);
    }

    [Fact]
    public void Unbind_StopsReceivingNotifications()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);

        binding.Bind(vm);
        binding.Unbind();

        var setCountBefore = proxy.SetValueCallCount;
        vm.Name = "New";

        Assert.Equal(setCountBefore, proxy.SetValueCallCount);
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        var vm = new SimpleViewModel { Name = "Old" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);

        binding.Bind(vm);
        binding.Dispose();

        vm.Name = "AfterDispose";
        Assert.Equal(1, proxy.SetValueCallCount); // Only initial binding
    }

    [Fact]
    public void Bind_NullSource_DoesNotThrow()
    {
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);

        binding.Bind(null);
        Assert.Equal(1, proxy.SetValueCallCount); // Triggers one SetValue(null)
    }
}
