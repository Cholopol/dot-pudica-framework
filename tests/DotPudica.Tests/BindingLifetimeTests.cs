using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>Verifies that long-lived binding infrastructure does not retain disposed data contexts.</summary>
public class BindingLifetimeTests
{
    [Fact]
    public void DisposedBindingContext_DoesNotRetainDataContext()
    {
        var context = CreateDisposedContext(out var viewModelReference);

        ForceCollection();

        Assert.False(viewModelReference.IsAlive);
        GC.KeepAlive(context);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BindingContext CreateDisposedContext(out WeakReference viewModelReference)
    {
        var viewModel = new SimpleViewModel { Name = "released" };
        var context = new BindingContext { DataContext = viewModel };
        viewModelReference = new WeakReference(viewModel);
        context.Dispose();
        return context;
    }

    private static void ForceCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact]
    public void PropertyBinding_DisposeThenBind_ThrowsObjectDisposedException()
    {
        var vm = new SimpleViewModel { Name = "Initial" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, PropertyNamePath(), BindingMode.OneWay);

        binding.Bind(vm);
        binding.Dispose();

        Assert.Throws<ObjectDisposedException>(() => binding.Bind(vm));
    }

    [Fact]
    public void PropertyBinding_DisposeThenUnbind_ThrowsObjectDisposedException()
    {
        var vm = new SimpleViewModel { Name = "Initial" };
        var proxy = new StubTargetProxy<string>();
        var binding = new PropertyBinding<string, string>(proxy, PropertyNamePath(), BindingMode.OneWay);

        binding.Bind(vm);
        binding.Dispose();

        Assert.Throws<ObjectDisposedException>(() => binding.Unbind());
    }

    [Fact]
    public void CommandBinding_DisposeThenBind_ThrowsObjectDisposedException()
    {
        var vm = new CommandViewModel();
        var binding = new CommandBinding(
            CommandPath(),
            parameterPath: null,
            triggerSubscribe: () => { },
            triggerUnsubscribe: () => { });

        binding.Bind(vm);
        binding.Dispose();

        Assert.Throws<ObjectDisposedException>(() => binding.Bind(vm));
    }

    [Fact]
    public void CollectionBinding_DisposeThenBind_ThrowsObjectDisposedException()
    {
        var vm = new CollectionViewModel();
        var proxy = new StubItemsTargetProxy();
        var binding = new CollectionBinding(proxy, CollectionItemsPath());

        binding.Bind(vm);
        binding.Dispose();

        Assert.Throws<ObjectDisposedException>(() => binding.Bind(vm));
    }

    [Fact]
    public void VirtualizedCollectionBinding_DisposeThenBind_ThrowsObjectDisposedException()
    {
        var vm = new CollectionViewModel();
        var proxy = new StubVirtualizedItemsTargetProxy();
        var binding = new VirtualizedCollectionBinding(proxy, CollectionItemsPath());

        binding.Bind(vm);
        binding.Dispose();

        Assert.Throws<ObjectDisposedException>(() => binding.Bind(vm));
    }

    private static TypedBindingPath<SimpleViewModel, string> PropertyNamePath()
        => BindingPathFactory.Create(
            static (SimpleViewModel vm) => vm.Name,
            static (vm, v) => vm.Name = v,
            "Name");

    private static TypedBindingPath<CommandViewModel, ICommand?> CommandPath()
        => BindingPathFactory.Create(
            static (CommandViewModel vm) => vm.Command,
            static (vm, v) => vm.Command = v,
            "Command");

    private static TypedBindingPath<CollectionViewModel, ObservableCollection<string>> CollectionItemsPath()
        => BindingPathFactory.Create(
            static (CollectionViewModel vm) => vm.Items,
            static (vm, v) => vm.Items = v,
            "Items");
}
