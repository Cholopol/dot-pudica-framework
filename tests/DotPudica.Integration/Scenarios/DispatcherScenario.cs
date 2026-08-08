using DotPudica.Core.ViewModels;
using DotPudica.Core.Binding;
using DotPudica.Godot;
using DotPudica.Godot.Views;
using Godot;
using DotPudica.Integration.Controls;
using DotPudica.Integration.Fixtures;

namespace DotPudica.Integration.Scenarios;

/// <summary>After modifying the source property from a background thread, target proxy reads/writes must occur on the Runner main thread.</summary>
public sealed class DispatcherScenario : IIntegrationScenario
{
    public string Name => "WorkerPathUpdate_IsAppliedOnMainThread";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var mainThreadId = System.Environment.CurrentManagedThreadId;
        var tracking = new ThreadTrackingControl { Name = "Tracking" };
        host.AddChild(tracking);

        var runtime = new DotPudicaViewRuntime<IntegrationTitleViewModel>();
        runtime.CaptureUiContext();
        var vm = new IntegrationTitleViewModel { Title = "before" };
        runtime.SetViewModel(vm, ViewModelOwnership.Owned);
        var path = new TypedBindingPath<IntegrationTitleViewModel, string>(
            static x => x.Title,
            static (x, v) => x.Title = v,
            ["Title"]);
        var proxy = new DelegateTargetProxy<ThreadTrackingControl, string>(
            tracking,
            static c => c.Text,
            static (c, v) => c.Text = v);
        runtime.BindProperty(proxy, path, BindingMode.OneWay);

        await IntegrationTestHelpers.WaitProcessFrame(host);
        var baselineAccess = tracking.AccessCount;

        await Task.Run(() => vm.Title = "from-worker");
        await IntegrationTestHelpers.WaitFrames(host, 2);

        try
        {
            if (tracking.Text != "from-worker")
                return IntegrationResult.Fail(Name, $"Expected Text=from-worker, actual={tracking.Text}");

            if (tracking.AccessCount <= baselineAccess)
                return IntegrationResult.Fail(Name, "No target write observed after background update");

            var workerWrites = tracking.SetThreadIds.Skip(baselineAccess).ToList();
            if (workerWrites.Count == 0)
                return IntegrationResult.Fail(Name, "Missing write thread records for background update");

            if (workerWrites.Any(id => id != mainThreadId))
                return IntegrationResult.Fail(Name,
                    $"Target write threads {string.Join(",", workerWrites)} do not match main thread {mainThreadId}");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            runtime.Dispose();
            tracking.QueueFree();
            await IntegrationTestHelpers.WaitProcessFrame(host);
        }
    }
}
