using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.Shared.Services;

public interface IShowcaseMatchService
{
    Task<ShowcaseMatchResult> MatchRoomAsync(CancellationToken cancellationToken);
    int StartedCount { get; }
    int CompletedCount { get; }
    int CancelledCount { get; }
}

/// <summary>Configurable delay fake match service, reused by MiniGame and Probe F.</summary>
public sealed class FakeShowcaseMatchService : IShowcaseMatchService
{
    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(2);

    private int _started;
    private int _completed;
    private int _cancelled;

    public int StartedCount => _started;
    public int CompletedCount => _completed;
    public int CancelledCount => _cancelled;

    public async Task<ShowcaseMatchResult> MatchRoomAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _started);
        try
        {
            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _completed);
            return new ShowcaseMatchResult("showcase-room-1", 2);
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _cancelled);
            throw;
        }
    }
}
