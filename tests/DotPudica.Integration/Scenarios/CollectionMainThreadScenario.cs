using System.Collections.ObjectModel;
using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;
using DotPudica.Integration.Controls;
using Godot;
using Samples.Showcase.Shared.Probes;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// CollectionBinding main-thread synchronization (in-memory stub proxy). See ItemsSourceContainerScenario for real container/scene instantiation.
/// </summary>
public sealed class CollectionMainThreadScenario : IIntegrationScenario
{
    public string Name => "CollectionBinding_SyncsAddRemoveOnMainThread";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var mainThreadId = System.Environment.CurrentManagedThreadId;
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);

        var proxy = new ThreadTrackingItemsProxy();
        var context = new BindingContext();
        context.SetUiDispatcher(dispatcher);

        var vm = new CollectionMainThreadFixtureViewModel();
        var path = new TypedBindingPath<CollectionMainThreadFixtureViewModel, ObservableCollection<string>>(
            static x => x.Items,
            null,
            ["Items"]);
        context.AddBinding(new CollectionBinding(proxy, path, dispatcher));
        context.DataContext = vm;
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var baseline = proxy.MutationThreadIds.Count;

        vm.Items.Add("a");
        vm.Items.Add("b");
        vm.Items.RemoveAt(0);
        await IntegrationTestHelpers.WaitFrames(host, 2);

        try
        {
            if (proxy.Items.Count != 1 || proxy.Items[0] is not "b")
                return IntegrationResult.Fail(Name, $"Expected [b], actual count={proxy.Items.Count}");

            var mutations = proxy.MutationThreadIds.Skip(baseline).ToList();
            if (mutations.Count == 0)
                return IntegrationResult.Fail(Name, "No collection target mutation observed");

            if (mutations.Any(id => id != mainThreadId))
                return IntegrationResult.Fail(Name,
                    $"Collection mutation threads {string.Join(",", mutations)} do not match main thread {mainThreadId}");

            var coalesce = new CoalesceProbe();
            coalesce.Reset("ok");
            coalesce.OnTargetWrite("ok");
            var eval = coalesce.Evaluate();
            if (eval.Verdict != ProbeVerdict.Pass)
                return IntegrationResult.Fail(Name, "CoalesceProbe self-check failed: " + eval.Observed);

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            context.Dispose();
        }
    }

    private partial class CollectionMainThreadFixtureViewModel : ViewModelBase
    {
        public ObservableCollection<string> Items { get; } = new();
    }
}
