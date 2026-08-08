namespace Samples.Showcase.Shared.Probes;

/// <summary>A: Records binding target write thread; asserts that off-main-thread writes count is 0.</summary>
public sealed class ThreadAffinityProbe : IProbe
{
    public string Id => "A";
    public string Name => "Thread affinity";

    private readonly List<int> _writeThreadIds = new();
    private int _mainThreadId = -1;

    public void Reset(int mainThreadId)
    {
        _mainThreadId = mainThreadId;
        lock (_writeThreadIds)
            _writeThreadIds.Clear();
    }

    public void RecordWrite()
    {
        var id = Environment.CurrentManagedThreadId;
        lock (_writeThreadIds)
            _writeThreadIds.Add(id);
    }

    public ProbeResult Evaluate()
    {
        int[] ids;
        lock (_writeThreadIds)
            ids = _writeThreadIds.ToArray();

        var offMain = ids.Count(id => id != _mainThreadId);
        var observed = $"writes={ids.Length}, offMain={offMain}, main={_mainThreadId}";
        var verdict = offMain == 0 && ids.Length > 0 ? ProbeVerdict.Pass : ProbeVerdict.Fail;
        if (ids.Length == 0)
            verdict = ProbeVerdict.Fail;

        return new ProbeResult(Id, Name, "off-main writes == 0 and at least one write", observed, verdict);
    }
}
