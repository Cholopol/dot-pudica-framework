using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>_ExitTree cancels in-flight match and drops late UI updates.</summary>
public sealed class MatchCancelScenario : IIntegrationScenario
{
    public string Name => "ExitTree_CancelsMatchAndDropsUiUpdate";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var view = new MatchCancelView { Name = "MatchCancelView" };
        host.AddChild(view);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var vm = view.PanelViewModel
            ?? throw new InvalidOperationException("MatchCancelViewModel not initialized");
        var service = view.MatchService
            ?? throw new InvalidOperationException("FakeMatchCancelService not initialized");

        if (vm.MatchCommand.CanExecute(null))
            vm.MatchCommand.Execute(null);

        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (service.StartedCount == 0)
            return IntegrationResult.Fail(Name, "match request did not start");

        var appliedBefore = vm.AppliedResultCount;
        view.QueueFree();
        await IntegrationTestHelpers.WaitFrames(host, 3);

        await service.Finished.WaitAsync(TimeSpan.FromSeconds(2));
        await IntegrationTestHelpers.WaitFrames(host, 2);

        if (vm.AppliedResultCount != appliedBefore)
            return IntegrationResult.Fail(Name,
                $"result written after cancel: before={appliedBefore}, after={vm.AppliedResultCount}");

        if (vm.MatchState is AsyncOperationState.Succeeded)
            return IntegrationResult.Fail(Name, "match state became Succeeded after ExitTree");

        return IntegrationResult.Pass(Name);
    }
}
