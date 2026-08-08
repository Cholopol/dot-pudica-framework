namespace Samples.Showcase.Shared.Probes;

/// <summary>D: IUiDispatcher.Post FIFO and frame delay statistics.</summary>
public sealed class DispatchOrderProbe : IProbe
{
    public string Id => "D";
    public string Name => "Dispatch order";

    private readonly List<(int Seq, long PostFrame, long ExecFrame)> _events = new();
    public int PostedCount { get; private set; }

    public void Reset()
    {
        PostedCount = 0;
        lock (_events)
            _events.Clear();
    }

    public void RecordPosted() => PostedCount++;

    public void RecordExecuted(int seq, long postFrame, long execFrame)
    {
        lock (_events)
            _events.Add((seq, postFrame, execFrame));
    }

    public ProbeResult Evaluate()
    {
        (int Seq, long PostFrame, long ExecFrame)[] events;
        lock (_events)
            events = _events.ToArray();

        var fifo = true;
        for (var i = 0; i < events.Length; i++)
        {
            if (events[i].Seq != i + 1)
            {
                fifo = false;
                break;
            }
        }

        var delays = events.Select(e => e.ExecFrame - e.PostFrame).OrderBy(d => d).ToArray();
        double p50 = Percentile(delays, 0.50);
        double p95 = Percentile(delays, 0.95);
        long max = delays.Length == 0 ? 0 : delays[^1];

        var complete = events.Length == PostedCount && PostedCount > 0;
        var observed =
            $"posted={PostedCount}, executed={events.Length}, fifo={fifo}, delayFrames p50={p50:0.##} p95={p95:0.##} max={max}";
        return new ProbeResult(
            Id,
            Name,
            "strict FIFO sequence; all posts executed",
            observed,
            complete && fifo ? ProbeVerdict.Pass : ProbeVerdict.Fail);
    }

    private static double Percentile(long[] sorted, double p)
    {
        if (sorted.Length == 0)
            return 0;
        var idx = (int)Math.Ceiling(p * sorted.Length) - 1;
        idx = Math.Clamp(idx, 0, sorted.Length - 1);
        return sorted[idx];
    }
}
