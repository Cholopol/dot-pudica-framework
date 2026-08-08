using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Binding;
using DotPudica.Core.Threading;
using DotPudica.Core.ViewModels;
using Samples.Showcase.Shared.Models;
using Samples.Showcase.Shared.Services;

namespace Samples.Showcase.MiniGame.Match;

/// <summary>Match VM with dispatcher + generation token cancel semantics.</summary>
public partial class MatchViewModel : ViewModelBase
{
    private readonly IShowcaseMatchService _matchService;
    private readonly IUiDispatcher _dispatcher;
    private readonly SceneOperationScope _sceneScope;
    private CancellationTokenSource? _matchCts;
    private long _matchGeneration;

    public MatchViewModel(
        IShowcaseMatchService matchService,
        SceneOperationScope sceneScope,
        IUiDispatcher? dispatcher = null)
    {
        _matchService = matchService;
        _sceneScope = sceneScope;
        _dispatcher = dispatcher ?? UiDispatcher.Immediate;
    }

    [ObservableProperty]
    private AsyncOperationState _matchState = AsyncOperationState.Idle;

    [ObservableProperty]
    private string _statusText = "Idle";

    [ObservableProperty]
    private string? _roomId;

    [ObservableProperty]
    private string? _errorText;

    public event Action<ShowcaseMatchResult>? MatchSucceeded;

    [RelayCommand(CanExecute = nameof(CanMatch))]
    private async Task MatchAsync()
    {
        var generation = Interlocked.Increment(ref _matchGeneration);
        _matchCts?.Cancel();
        _matchCts?.Dispose();
        _matchCts = _sceneScope.CreateLinkedTokenSource();

        ApplyState(AsyncOperationState.Running, "Matching…", roomId: null, error: null);

        try
        {
            var result = await _matchService.MatchRoomAsync(_matchCts.Token).ConfigureAwait(false);
            PostCurrent(generation, () =>
            {
                RoomId = result.RoomId;
                MatchState = AsyncOperationState.Succeeded;
                StatusText = $"Matched: {result.RoomId}, {result.PlayerCount} players";
                ErrorText = null;
                MatchCommand.NotifyCanExecuteChanged();
                MatchSucceeded?.Invoke(result);
            });
        }
        catch (OperationCanceledException)
        {
            PostCurrent(generation, () =>
            {
                MatchState = AsyncOperationState.Cancelled;
                StatusText = "Cancelled";
                ErrorText = null;
                MatchCommand.NotifyCanExecuteChanged();
            });
        }
        catch (Exception ex)
        {
            PostCurrent(generation, () =>
            {
                MatchState = AsyncOperationState.Failed;
                StatusText = "Match failed";
                ErrorText = ex.Message;
                MatchCommand.NotifyCanExecuteChanged();
            });
        }
    }

    private void PostCurrent(long generation, Action action)
    {
        _dispatcher.Post(() =>
        {
            if (IsDisposed || generation != Interlocked.Read(ref _matchGeneration))
                return;

            action();
        });
    }

    private bool CanMatch() => MatchState is not AsyncOperationState.Running;

    private void ApplyState(AsyncOperationState state, string status, string? roomId, string? error)
    {
        MatchState = state;
        StatusText = status;
        RoomId = roomId;
        ErrorText = error;
        MatchCommand.NotifyCanExecuteChanged();
    }

    protected override void OnDispose()
    {
        Interlocked.Increment(ref _matchGeneration);
        _matchCts?.Cancel();
        _matchCts?.Dispose();
        _matchCts = null;
        base.OnDispose();
    }
}
