using System.Collections;
using System.Collections.ObjectModel;
using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

public class VirtualizedCollectionBindingTests
{
    private static TypedBindingPath<CollectionViewModel, ObservableCollection<string>> ItemsPath()
        => BindingPathFactory.Create(
            static (CollectionViewModel vm) => vm.Items,
            static (vm, v) => vm.Items = v,
            "Items");

    [Fact]
    public void Bind_AssignsSourceListWithoutCreatingItemOperations()
    {
        var viewModel = new CollectionViewModel();
        viewModel.Items.Add("A");
        var proxy = new StubVirtualizedItemsTargetProxy();
        var path = ItemsPath();
        var binding = new VirtualizedCollectionBinding(proxy, path);

        binding.Bind(viewModel);

        Assert.Same(viewModel.Items, proxy.Items);
    }

    [Fact]
    public void CollectionChange_RefreshesVirtualTarget()
    {
        var viewModel = new CollectionViewModel();
        var proxy = new StubVirtualizedItemsTargetProxy();
        var path = ItemsPath();
        var binding = new VirtualizedCollectionBinding(proxy, path);
        binding.Bind(viewModel);

        viewModel.Items.Add("A");

        Assert.Equal(1, proxy.RefreshCount);
    }

    [Fact]
    public void CollectionChanges_BeforeQueuedDispatch_CoalesceToOneRefresh()
    {
        var viewModel = new CollectionViewModel();
        var proxy = new StubVirtualizedItemsTargetProxy();
        var dispatcher = new QueuedUiDispatcher();
        var path = ItemsPath();
        var binding = new VirtualizedCollectionBinding(proxy, path, dispatcher);
        binding.Bind(viewModel);
        dispatcher.RunAll();

        dispatcher.HasAccess = false;
        viewModel.Items.Add("A");
        viewModel.Items.Add("B");
        viewModel.Items.Add("C");
        dispatcher.RunAll();

        Assert.Equal(1, proxy.RefreshCount);
    }

    [Fact]
    public async Task SourcePathChange_FromWorkerThread_ReadsCollectionOnUiDispatcher()
    {
        var items = new ObservableCollection<string> { "A" };
        var path = new ThreadTrackingVirtualPath(items);
        var proxy = new StubVirtualizedItemsTargetProxy();
        var dispatcher = new QueuedUiDispatcher();
        var binding = new VirtualizedCollectionBinding(proxy, path, dispatcher);
        binding.Bind(new object());
        path.LastGetValueThreadId = 0;

        await Task.Run(path.RaiseValueChanged);

        Assert.Equal(0, path.LastGetValueThreadId);
        var dispatcherThreadId = Environment.CurrentManagedThreadId;
        dispatcher.RunAll();
        Assert.Equal(dispatcherThreadId, path.LastGetValueThreadId);
    }

    [Theory]
    [InlineData(100, 20, 0, 100, 1, 0, 6)]
    [InlineData(100, 20, 40, 100, 1, 1, 8)]
    [InlineData(3, 20, 40, 100, 1, 1, 3)]
    public void RangeCalculator_SelectsVisibleWindow(
        int itemCount,
        float itemHeight,
        float scrollOffset,
        float viewportHeight,
        int overscan,
        int expectedStart,
        int expectedEnd)
    {
        var range = VirtualizedItemRangeCalculator.Calculate(
            itemCount, itemHeight, scrollOffset, viewportHeight, overscan);

        Assert.Equal(new VirtualizedItemRange(expectedStart, expectedEnd), range);
    }

    private sealed class ThreadTrackingVirtualPath(IList items) : IBindingPath
    {
        public int LastGetValueThreadId { get; set; }

        public event EventHandler? ValueChanged;

        public void Bind(object? source) { }

        public void Unbind() { }

        public object? GetValue()
        {
            LastGetValueThreadId = Environment.CurrentManagedThreadId;
            return items;
        }

        public bool SetValue(object? value) => false;

        public void Dispose() { }

        public void RaiseValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}
