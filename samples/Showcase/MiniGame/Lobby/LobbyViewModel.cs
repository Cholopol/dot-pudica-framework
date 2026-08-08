using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using DotPudica.Core.ViewModels;
using Samples.Showcase.Shared.Models;
using Samples.Showcase.Shared.Services;

namespace Samples.Showcase.MiniGame.Lobby;

/// <summary>Lobby VM: drain LatestSnapshotMailbox on UI thread.</summary>
public partial class LobbyViewModel : ViewModelBase
{
    private readonly IRoomService _roomService;
    private readonly LatestSnapshotMailbox<RoomSnapshot> _mailbox = new();
    private readonly IUiDispatcher _dispatcher;

    public LobbyViewModel(IRoomService roomService, IUiDispatcher? dispatcher = null)
    {
        _roomService = roomService;
        _dispatcher = dispatcher ?? UiDispatcher.Immediate;
        _roomService.SnapshotPublished += OnSnapshotPublished;
    }

    public ObservableCollection<RoomInfo> Rooms { get; } = new();

    [ObservableProperty]
    private string _statusText = "Waiting for rooms…";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EnterLoadoutCommand))]
    private long _snapshotSequence;

    public event Action? EnterLoadoutRequested;

    private void OnSnapshotPublished(RoomSnapshot snapshot) => _mailbox.Publish(snapshot);

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

            SnapshotSequence = snapshot.Sequence;
            StatusText = $"Rooms {Rooms.Count} · seq {snapshot.Sequence}";
        });
    }

    private bool CanEnterLoadout() => Rooms.Count > 0;

    [RelayCommand(CanExecute = nameof(CanEnterLoadout))]
    private void EnterLoadout() => EnterLoadoutRequested?.Invoke();

    protected override void OnDispose()
    {
        _roomService.SnapshotPublished -= OnSnapshotPublished;
        base.OnDispose();
    }
}
