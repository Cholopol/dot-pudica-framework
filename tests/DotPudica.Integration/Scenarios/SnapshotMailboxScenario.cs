using DotPudica.Core.Binding;
using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>High-frequency snapshots apply only the newest state.</summary>
public sealed class SnapshotMailboxScenario : IIntegrationScenario
{
    public string Name => "LatestSnapshot_AppliesOnlyNewest";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);
        var vm = new SnapshotMailboxViewModel(dispatcher);

        await Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                vm.PublishFromNetwork(new SnapshotLobbySnapshot(
                [
                    new SnapshotRoomInfo($"r{i}", $"Room {i}", i % 5)
                ]));
            }
        });

        vm.DrainOnUiThread();
        await IntegrationTestHelpers.WaitFrames(host, 2);

        if (vm.AppliedSnapshotCount != 1)
            return IntegrationResult.Fail(Name, $"expected 1 applied snapshot, got {vm.AppliedSnapshotCount}");

        if (vm.Rooms.Count != 1 || vm.Rooms[0].Id != "r99")
            return IntegrationResult.Fail(Name, $"expected latest room r99, got {vm.Rooms.FirstOrDefault()?.Id}");

        return IntegrationResult.Pass(Name);
    }
}
