using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.Threading;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DotPudica.Integration.Fixtures;

public interface IMatchCancelService
{
    Task<MatchCancelResult> MatchRoomAsync(CancellationToken cancellationToken);
    int StartedCount { get; }
}

public sealed record MatchCancelResult(string RoomId, int PlayerCount);

public sealed class FakeMatchCancelService : IMatchCancelService
{
    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(30);

    private int _started;
    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int StartedCount => _started;
    public Task Finished => _finished.Task;

    public async Task<MatchCancelResult> MatchRoomAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _started);
        try
        {
            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
            return new MatchCancelResult("room-1", 2);
        }
        finally
        {
            _finished.TrySetResult();
        }
    }
}

public partial class MatchCancelViewModel : ViewModelBase
{
    private readonly IMatchCancelService _matchService;
    private readonly IUiDispatcher _dispatcher;
    private readonly SceneOperationScope _sceneScope;
    private CancellationTokenSource? _matchCts;
    private long _matchGeneration;

    public MatchCancelViewModel(
        IMatchCancelService matchService,
        SceneOperationScope sceneScope,
        IUiDispatcher dispatcher)
    {
        _matchService = matchService;
        _sceneScope = sceneScope;
        _dispatcher = dispatcher;
    }

    [ObservableProperty]
    private AsyncOperationState _matchState = AsyncOperationState.Idle;

    [ObservableProperty]
    private string _statusText = "idle";

    [ObservableProperty]
    private string? _roomId;

    public int AppliedResultCount { get; private set; }

    [RelayCommand(CanExecute = nameof(CanMatch))]
    private async Task MatchAsync()
    {
        var generation = Interlocked.Increment(ref _matchGeneration);
        _matchCts?.Cancel();
        _matchCts?.Dispose();
        _matchCts = _sceneScope.CreateLinkedTokenSource();

        MatchState = AsyncOperationState.Running;
        StatusText = "matching";
        MatchCommand.NotifyCanExecuteChanged();

        try
        {
            var result = await _matchService.MatchRoomAsync(_matchCts.Token).ConfigureAwait(false);
            _dispatcher.Post(() =>
            {
                if (IsDisposed || generation != Interlocked.Read(ref _matchGeneration))
                    return;

                AppliedResultCount++;
                RoomId = result.RoomId;
                MatchState = AsyncOperationState.Succeeded;
                StatusText = "joined";
                MatchCommand.NotifyCanExecuteChanged();
            });
        }
        catch (OperationCanceledException)
        {
            _dispatcher.Post(() =>
            {
                if (IsDisposed || generation != Interlocked.Read(ref _matchGeneration))
                    return;

                MatchState = AsyncOperationState.Cancelled;
                StatusText = "cancelled";
                MatchCommand.NotifyCanExecuteChanged();
            });
        }
    }

    private bool CanMatch() => MatchState is not AsyncOperationState.Running;

    protected override void OnDispose()
    {
        Interlocked.Increment(ref _matchGeneration);
        _matchCts?.Cancel();
        _matchCts?.Dispose();
        _matchCts = null;
        base.OnDispose();
    }
}

[DotPudicaView(typeof(MatchCancelViewModel))]
public partial class MatchCancelView : Control
{
    private readonly SceneOperationScope _sceneScope = new();
    private FakeMatchCancelService? _matchService;

    [Export, BindTo(nameof(MatchCancelViewModel.StatusText))]
    private Label _statusLabel = null!;

    [Export, BindTo(nameof(MatchCancelViewModel.RoomId))]
    private Label _roomLabel = null!;

    [Export, BindCommand(nameof(MatchCancelViewModel.MatchCommand))]
    private Button _matchButton = null!;

    public FakeMatchCancelService? MatchService => _matchService;
    public MatchCancelViewModel? PanelViewModel => ViewModel;

    [ViewModelFactory]
    private MatchCancelViewModel CreateMatchViewModel()
    {
        _matchService = new FakeMatchCancelService { Delay = TimeSpan.FromSeconds(30) };
        var dispatcher = UiDispatcher.FromSynchronizationContext(
            Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext"));
        return new MatchCancelViewModel(_matchService, _sceneScope, dispatcher);
    }

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady()
    {
        _statusLabel ??= new Label { Name = "StatusLabel" };
        if (_statusLabel.GetParent() is null)
            AddChild(_statusLabel);

        _roomLabel ??= new Label { Name = "RoomLabel" };
        if (_roomLabel.GetParent() is null)
            AddChild(_roomLabel);

        _matchButton ??= new Button { Name = "MatchButton", Text = "Match" };
        if (_matchButton.GetParent() is null)
            AddChild(_matchButton);
    }

    partial void OnViewDisposing()
    {
        _sceneScope.Cancel();
        _sceneScope.Dispose();
    }
}
