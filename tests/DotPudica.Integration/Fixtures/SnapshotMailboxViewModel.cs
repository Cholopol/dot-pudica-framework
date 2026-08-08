using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using DotPudica.Core.ViewModels;

namespace DotPudica.Integration.Fixtures;

public sealed record SnapshotRoomInfo(string Id, string Title, int PlayerCount);

public sealed record SnapshotLobbySnapshot(IReadOnlyList<SnapshotRoomInfo> Rooms);

/// <summary>Integration fixture for LatestSnapshotMailbox drain semantics.</summary>
public partial class SnapshotMailboxViewModel : ViewModelBase
{
    private readonly LatestSnapshotMailbox<SnapshotLobbySnapshot> _mailbox = new();
    private readonly IUiDispatcher _dispatcher;

    public SnapshotMailboxViewModel(IUiDispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? UiDispatcher.Immediate;
    }

    public ObservableCollection<SnapshotRoomInfo> Rooms { get; } = new();

    [ObservableProperty]
    private int _appliedSnapshotCount;

    public void PublishFromNetwork(SnapshotLobbySnapshot snapshot) => _mailbox.Publish(snapshot);

    public void DrainOnUiThread()
    {
        if (IsDisposed)
            return;

        if (!_mailbox.TryDrainLatest(out var snapshot) || snapshot is null)
            return;

        _dispatcher.Post(() =>
        {
            if (IsDisposed)
                return;

            Rooms.Clear();
            foreach (var room in snapshot.Rooms)
                Rooms.Add(room);
            AppliedSnapshotCount++;
        });
    }
}
