using System.Collections;
using System.Collections.ObjectModel;
using DotPudica.Core.Binding;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// CollectionBinding unit tests. Verifies ObservableCollection add/remove/update synchronization with the target proxy.
/// </summary>
public class CollectionBindingTests
{
    private static TypedBindingPath<CollectionViewModel, ObservableCollection<string>> ItemsPath()
        => BindingPathFactory.Create(
            static (CollectionViewModel vm) => vm.Items,
            static (vm, v) => vm.Items = v,
            "Items");

    [Fact]
    public void Bind_InitialCollection_SyncsAllItems()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        vm.Items.Add("B");
        vm.Items.Add("C");

        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);

        binding.Bind(vm);

        Assert.Equal(3, proxy.Items.Count);
        Assert.Equal("A", proxy.Items[0]);
        Assert.Equal("B", proxy.Items[1]);
        Assert.Equal("C", proxy.Items[2]);
    }

    [Fact]
    public void Bind_EmptyCollection_NoItems()
    {
        var vm = new CollectionViewModel();
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);

        binding.Bind(vm);

        Assert.Empty(proxy.Items);
    }

    [Fact]
    public void Dispatcher_DropsQueuedCollectionSyncAfterUnbind()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        var proxy = new StubItemsTargetProxy();
        var dispatcher = new QueuedUiDispatcher();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path, dispatcher);

        binding.Bind(vm);
        binding.Unbind();
        dispatcher.RunAll();

        Assert.Empty(proxy.Items);
    }

    [Fact]
    public void Dispatcher_DefersInitialCollectionSync()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        var proxy = new StubItemsTargetProxy();
        var dispatcher = new QueuedUiDispatcher();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path, dispatcher);

        binding.Bind(vm);
        Assert.Empty(proxy.Items);
        dispatcher.RunAll();
        Assert.Equal(new[] { "A" }, proxy.Items);
    }

    [Fact]
    public void Add_Item_TriggersProxyAdd()
    {
        var vm = new CollectionViewModel();
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        vm.Items.Add("First");

        Assert.Single(proxy.Items);
        Assert.Equal("First", proxy.Items[0]);
        Assert.Contains(proxy.Operations, op => op.Kind == ProxyOpKind.Add && op.Index == 0);
    }

    [Fact]
    public void Add_MultipleItems_AppendsInOrder()
    {
        var vm = new CollectionViewModel();
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        vm.Items.Add("X");
        vm.Items.Add("Y");
        vm.Items.Add("Z");

        Assert.Equal(new[] { "X", "Y", "Z" }, proxy.Items.ToArray());
    }

    [Fact]
    public void Insert_AtIndex_TriggersProxyAddAtIndex()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        vm.Items.Add("C");
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        vm.Items.Insert(1, "B");

        Assert.Equal(new[] { "A", "B", "C" }, proxy.Items.ToArray());
    }

    [Fact]
    public void Remove_Item_TriggersProxyRemoveAt()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        vm.Items.Add("B");
        vm.Items.Add("C");
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        vm.Items.RemoveAt(1);

        Assert.Equal(new[] { "A", "C" }, proxy.Items.ToArray());
        Assert.Contains(proxy.Operations, op => op.Kind == ProxyOpKind.RemoveAt && op.Index == 1);
    }

    [Fact]
    public void Move_Item_TriggersProxyMove()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        vm.Items.Add("B");
        vm.Items.Add("C");
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        vm.Items.Move(0, 2);

        Assert.Equal(new[] { "B", "C", "A" }, proxy.Items.ToArray());
        Assert.Contains(proxy.Operations, op => op.Kind == ProxyOpKind.Move);
    }

    [Fact]
    public void Replace_Item_TriggersRemoveAtAndAdd()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        vm.Items.Add("B");
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        vm.Items[1] = "B2";

        Assert.Equal(new[] { "A", "B2" }, proxy.Items.ToArray());
    }

    [Fact]
    public void Clear_Collection_TriggersProxyClear()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        vm.Items.Add("B");
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        vm.Items.Clear();

        Assert.Empty(proxy.Items);
        Assert.Contains(proxy.Operations, op => op.Kind == ProxyOpKind.Clear);
    }

    [Fact]
    public void SourcePath_Change_TriggersFullResync()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("Old1");
        vm.Items.Add("Old2");
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        // Replace the entire collection
        vm.Items = new ObservableCollection<string> { "New1", "New2", "New3" };

        Assert.Equal(new[] { "New1", "New2", "New3" }, proxy.Items.ToArray());
    }

    [Fact]
    public void SourcePath_Change_DetachesOldCollection()
    {
        var vm = new CollectionViewModel();
        var oldCollection = vm.Items;
        oldCollection.Add("Old");
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        var newCollection = new ObservableCollection<string> { "New" };
        vm.Items = newCollection;

        // Modifying the old collection should not affect the proxy
        var countBefore = proxy.Items.Count;
        oldCollection.Add("Stale");
        Assert.Equal(countBefore, proxy.Items.Count);

        // Modifying the new collection should sync
        newCollection.Add("Extra");
        Assert.Equal(new[] { "New", "Extra" }, proxy.Items.ToArray());
    }

    [Fact]
    public void Unbind_StopsReceivingNotifications()
    {
        var vm = new CollectionViewModel();
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        binding.Unbind();

        var countBefore = proxy.Items.Count;
        vm.Items.Add("AfterUnbind");

        Assert.Equal(countBefore, proxy.Items.Count);
    }

    [Fact]
    public void Unbind_ClearsTargetProxy()
    {
        var vm = new CollectionViewModel();
        vm.Items.Add("A");
        vm.Items.Add("B");
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        Assert.Equal(2, proxy.Items.Count);

        binding.Unbind();

        Assert.Empty(proxy.Items);
    }

    [Fact]
    public void Dispose_StopsReceivingNotifications()
    {
        var vm = new CollectionViewModel();
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        binding.Dispose();

        var countBefore = proxy.Items.Count;
        vm.Items.Add("AfterDispose");

        Assert.Equal(countBefore, proxy.Items.Count);
    }

    [Fact]
    public void Bind_NullSource_DoesNotThrow()
    {
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);

        binding.Bind(null);

        Assert.Empty(proxy.Items);
    }

    [Fact]
    public void Bind_NullCollection_DoesNotThrow()
    {
        var vm = new CollectionViewModel { Items = null! };
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);

        binding.Bind(vm);

        Assert.Empty(proxy.Items);
    }

    [Fact]
    public void AddRange_MultipleOperations_AllSynced()
    {
        var vm = new CollectionViewModel();
        var proxy = new StubItemsTargetProxy();
        var path = ItemsPath();
        var binding = new CollectionBinding(proxy, path);
        binding.Bind(vm);

        // Simulate batch operations: add, insert, remove, move
        vm.Items.Add("A");
        vm.Items.Add("B");
        vm.Items.Add("C");
        vm.Items.Insert(1, "X");
        vm.Items.RemoveAt(0);
        vm.Items.Move(0, 2);

        // Expected: Move(0,2) moves "X" from index 0 to index 2 → [B, C, X]
        Assert.Equal(new[] { "B", "C", "X" }, proxy.Items.ToArray());
    }

    [Fact]
    public async Task SourcePathChange_FromWorkerThread_ReadsCollectionOnUiDispatcher()
    {
        var items = new ObservableCollection<string> { "A" };
        var path = new ThreadTrackingCollectionPath(items);
        var proxy = new StubItemsTargetProxy();
        var dispatcher = new QueuedUiDispatcher();
        var binding = new CollectionBinding(proxy, path, dispatcher);
        binding.Bind(new object());
        path.LastGetValueThreadId = 0;

        await Task.Run(path.RaiseValueChanged);

        Assert.Equal(0, path.LastGetValueThreadId);
        var dispatcherThreadId = Environment.CurrentManagedThreadId;
        dispatcher.RunAll();
        Assert.Equal(dispatcherThreadId, path.LastGetValueThreadId);
    }

    private sealed class ThreadTrackingCollectionPath(IList items) : IBindingPath
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
