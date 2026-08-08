using System.Collections.ObjectModel;
using DotPudica.Core.Binding;
using Godot;
using DotPudica.Integration.Controls;
using DotPudica.Integration.Fixtures;

namespace DotPudica.Integration.Scenarios;

/// <summary>After a collection path replacement is triggered from a background thread, Reset-sync list reads and target mutations both occur on the main thread.</summary>
public sealed class CollectionScenario : IIntegrationScenario
{
    public string Name => "CollectionReset_DoesNotReadSourceOnWorkerThread";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var mainThreadId = System.Environment.CurrentManagedThreadId;
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);

        var proxy = new ThreadTrackingItemsProxy();
        var context = new BindingContext();
        context.SetUiDispatcher(dispatcher);

        var vm = new IntegrationListViewModel();
        var path = new TypedBindingPath<IntegrationListViewModel, ObservableCollection<string>>(
            static x => x.Items,
            static (x, v) => x.Items = v,
            ["Items"]);
        var binding = new CollectionBinding(proxy, path, dispatcher);
        context.AddBinding(binding);
        context.DataContext = vm;
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var baseline = proxy.MutationThreadIds.Count;
        var replacement = new ObservableCollection<string> { "x", "y", "z" };

        await Task.Run(() => vm.Items = replacement);
        await IntegrationTestHelpers.WaitFrames(host, 2);

        try
        {
            if (proxy.Items.Count != 3 || proxy.Items[0] is not "x")
                return IntegrationResult.Fail(Name, $"Expected sync to [x,y,z], actual count={proxy.Items.Count}");

            var mutations = proxy.MutationThreadIds.Skip(baseline).ToList();
            if (mutations.Count == 0)
                return IntegrationResult.Fail(Name, "No target mutation observed after path replacement");

            if (mutations.Any(id => id != mainThreadId))
                return IntegrationResult.Fail(Name,
                    $"Collection target mutation threads {string.Join(",", mutations)} do not match main thread {mainThreadId}");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            context.Dispose();
        }
    }
}
