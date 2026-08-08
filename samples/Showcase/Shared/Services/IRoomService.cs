using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.Shared.Services;

/// <summary>Simulated lobby heartbeat: publishes <see cref="RoomSnapshot"/> at a fixed rate from the background.</summary>
public interface IRoomService : IDisposable
{
    event Action<RoomSnapshot>? SnapshotPublished;
    void Start(int hertz = 20);
    void Stop();
    bool IsRunning { get; }
    long PublishedCount { get; }
}

public sealed class FakeRoomService : IRoomService
{
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private long _sequence;
    private long _published;

    public event Action<RoomSnapshot>? SnapshotPublished;
    public bool IsRunning => _cts is { IsCancellationRequested: false };
    public long PublishedCount => Interlocked.Read(ref _published);

    public void Start(int hertz = 20)
    {
        if (IsRunning)
            return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var delay = TimeSpan.FromMilliseconds(Math.Max(1, 1000.0 / Math.Max(1, hertz)));
        _loop = Task.Run(() => RunLoopAsync(delay, token), token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _loop?.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { }
        _cts?.Dispose();
        _cts = null;
        _loop = null;
    }

    private async Task RunLoopAsync(TimeSpan delay, CancellationToken token)
    {
        var rng = new Random(42);
        while (!token.IsCancellationRequested)
        {
            var seq = Interlocked.Increment(ref _sequence);
            var rooms = new List<RoomInfo>(8);
            var count = 4 + rng.Next(0, 5);
            for (var i = 0; i < count; i++)
            {
                rooms.Add(new RoomInfo(
                    $"r{seq}-{i}",
                    $"Room {i + 1}",
                    rng.Next(1, 5),
                    4));
            }

            var snapshot = new RoomSnapshot(rooms, seq);
            Interlocked.Increment(ref _published);
            SnapshotPublished?.Invoke(snapshot);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }
    }

    public void Dispose() => Stop();
}
