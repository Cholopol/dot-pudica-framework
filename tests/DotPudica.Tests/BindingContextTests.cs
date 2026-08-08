using System.Collections.ObjectModel;
using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// BindingContext unit tests. Verifies context switching, binding management, and cleanup.
/// </summary>
public class BindingContextTests
{
    private static TypedBindingPath<SimpleViewModel, string> NamePath()
        => BindingPathFactory.Create(
            static (SimpleViewModel vm) => vm.Name,
            static (vm, v) => vm.Name = v,
            "Name");

    private static TypedBindingPath<CollectionViewModel, ObservableCollection<string>> ItemsPath()
        => BindingPathFactory.Create(
            static (CollectionViewModel vm) => vm.Items,
            static (vm, v) => vm.Items = v,
            "Items");

    [Fact]
    public void DataContext_Set_TriggersDataContextChanged()
    {
        var ctx = new BindingContext();
        var triggered = false;
        ctx.DataContextChanged += (_, _) => triggered = true;

        ctx.DataContext = new SimpleViewModel();

        Assert.True(triggered);
    }

    [Fact]
    public void DataContext_SetSameValue_DoesNotTrigger()
    {
        var vm = new SimpleViewModel();
        var ctx = new BindingContext { DataContext = vm };
        var triggered = false;
        ctx.DataContextChanged += (_, _) => triggered = true;

        ctx.DataContext = vm;

        Assert.False(triggered);
    }

    [Fact]
    public void LifecycleOperation_OutsideUiThread_Throws()
    {
        var dispatcher = new QueuedUiDispatcher { HasAccess = false };
        var context = new BindingContext();
        context.SetUiDispatcher(dispatcher);

        Assert.Throws<InvalidOperationException>(() => context.DataContext = new SimpleViewModel());
    }

    [Fact]
    public void AddBinding_WithExistingContext_BindsImmediately()
    {
        var vm = new SimpleViewModel { Name = "Init" };
        var ctx = new BindingContext { DataContext = vm };

        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);

        ctx.AddBinding(binding);

        Assert.Equal("Init", proxy.SetValues.Last());
    }

    [Fact]
    public void DataContext_Change_RebindsAllBindings()
    {
        var vm1 = new SimpleViewModel { Name = "First" };
        var vm2 = new SimpleViewModel { Name = "Second" };
        var ctx = new BindingContext { DataContext = vm1 };

        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);
        ctx.AddBinding(binding);

        ctx.DataContext = vm2;

        Assert.Equal("Second", proxy.SetValues.Last());
    }

    [Fact]
    public void ClearBindings_DisposesAll()
    {
        var vm = new SimpleViewModel { Name = "Init" };
        var ctx = new BindingContext { DataContext = vm };

        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);
        ctx.AddBinding(binding);

        ctx.ClearBindings();

        var setCountBefore = proxy.SetValueCallCount;
        vm.Name = "AfterClear";

        Assert.Equal(setCountBefore, proxy.SetValueCallCount);
    }

    [Fact]
    public void RemoveBinding_DisposesSpecific()
    {
        var vm = new SimpleViewModel { Name = "Init" };
        var ctx = new BindingContext { DataContext = vm };

        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);
        ctx.AddBinding(binding);

        ctx.RemoveBinding(binding);

        var setCountBefore = proxy.SetValueCallCount;
        vm.Name = "AfterRemove";

        Assert.Equal(setCountBefore, proxy.SetValueCallCount);
    }

    [Fact]
    public void Dispose_ClearsAllBindings()
    {
        var vm = new SimpleViewModel { Name = "Init" };
        var ctx = new BindingContext { DataContext = vm };

        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, NamePath(), BindingMode.OneWay);
        ctx.AddBinding(binding);

        ctx.Dispose();

        var setCountBefore = proxy.SetValueCallCount;
        vm.Name = "AfterDispose";

        Assert.Equal(setCountBefore, proxy.SetValueCallCount);
    }

    [Fact]
    public void AddBinding_PropertyAndCommand_BothManaged()
    {
        var vm = new CommandViewModel();
        vm.Command = vm.DefaultCommand;
        var ctx = new BindingContext { DataContext = vm };

        var proxy = new StubTargetProxy<int>();
        var propPath = BindingPathFactory.Create(
            static (CommandViewModel x) => x.ExecuteCount,
            static (x, v) => x.ExecuteCount = v,
            "ExecuteCount");
        var propBinding = new PropertyBinding<int, int>(proxy, propPath, BindingMode.OneWay);
        ctx.AddBinding(propBinding);

        var subscribed = false;
        var cmdPath = BindingPathFactory.Create(
            static (CommandViewModel x) => x.Command,
            static (x, v) => x.Command = v,
            "Command");
        var cmdBinding = new CommandBinding(
            cmdPath, null,
            triggerSubscribe: () => subscribed = true,
            triggerUnsubscribe: () => subscribed = false);
        ctx.AddBinding(cmdBinding);

        // Command binding is subscribed (DefaultCommand is not null)
        Assert.True(subscribed);
        // Property binding initial sync
        Assert.Equal(1, proxy.SetValueCallCount);
        Assert.Equal(0, proxy.SetValues[0]);

        vm.ExecuteCount++;

        // Property binding receives change notification
        Assert.Equal(2, proxy.SetValueCallCount);
        Assert.Equal(1, proxy.SetValues[1]);
    }

    [Fact]
    public void AddBinding_Collection_BindsImmediately()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("Init");
        var ctx = new BindingContext { DataContext = vm };

        var proxy = new StubItemsTargetProxy();
        var binding = new CollectionBinding(proxy, ItemsPath());
        ctx.AddBinding(binding);

        Assert.Single(proxy.Items);
        Assert.Equal("Init", proxy.Items[0]);
    }

    [Fact]
    public void DataContext_Change_RebindsCollectionBinding()
    {
        var vm1 = new CollectionViewModel();
        vm1.Items.Add("Old");
        var ctx = new BindingContext { DataContext = vm1 };

        var proxy = new StubItemsTargetProxy();
        var binding = new CollectionBinding(proxy, ItemsPath());
        ctx.AddBinding(binding);

        var vm2 = new CollectionViewModel();
        vm2.Items.Add("New1");
        vm2.Items.Add("New2");
        ctx.DataContext = vm2;

        Assert.Equal(new[] { "New1", "New2" }, proxy.Items.ToArray());
    }

    [Fact]
    public void ClearBindings_DisposesCollectionBinding()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        var ctx = new BindingContext { DataContext = vm };

        var proxy = new StubItemsTargetProxy();
        var binding = new CollectionBinding(proxy, ItemsPath());
        ctx.AddBinding(binding);

        ctx.ClearBindings();

        var countBefore = proxy.Items.Count;
        vm.Items.Add("AfterClear");

        Assert.Equal(countBefore, proxy.Items.Count);
    }
}
