namespace Samples.Showcase.Shared.Probes;

/// <summary>E: Backpressure evidence — no PASS/FAIL verdict, yields per-frame execution count and completion frames.</summary>
public sealed class BackpressureProbe : IProbe
{
    public string Id => "E";
    public string Name => "Backpressure evidence";

    private readonly Dictionary<long, int> _perFrame = new();
    public int TotalPosted { get; private set; }
    public int TotalExecuted { get; private set; }
    public long FirstFrame { get; private set; } = -1;
    public long LastFrame { get; private set; } = -1;
    public string Mode { get; private set; } = "";

    public void Reset(string mode, int totalPosted)
    {
        Mode = mode;
        TotalPosted = totalPosted;
        TotalExecuted = 0;
        FirstFrame = -1;
        LastFrame = -1;
        lock (_perFrame)
            _perFrame.Clear();
    }

    public void RecordExecuted(long frame)
    {
        TotalExecuted++;
        if (FirstFrame < 0)
            FirstFrame = frame;
        LastFrame = frame;
        lock (_perFrame)
        {
            _perFrame.TryGetValue(frame, out var n);
            _perFrame[frame] = n + 1;
        }
    }

    public IReadOnlyDictionary<long, int> PerFrameCounts
    {
        get
        {
            lock (_perFrame)
                return new Dictionary<long, int>(_perFrame);
        }
    }

    public long FramesToComplete => FirstFrame < 0 || LastFrame < 0 ? 0 : LastFrame - FirstFrame + 1;

    public ProbeResult Evaluate()
    {
        int peak;
        lock (_perFrame)
            peak = _perFrame.Count == 0 ? 0 : _perFrame.Values.Max();

        var observed =
            $"mode={Mode}, posted={TotalPosted}, executed={TotalExecuted}, frames={FramesToComplete}, peakPerFrame={peak}";
        return new ProbeResult(
            Id,
            Name,
            "evidence table only — no FAIL verdict",
            observed,
            ProbeVerdict.Evidence);
    }
}
