namespace Samples.Showcase.Shared.Probes;

/// <summary>C: After rebind, stale deliveries must not write back old VM values.</summary>
public sealed class StaleWorkProbe : IProbe
{
    public string Id => "C";
    public string Name => "Stale delivery drop";

    private readonly List<string> _writes = new();
    public string? ExpectedFinal { get; private set; }
    public string? ForbiddenPrefix { get; private set; }

    public void Reset(string expectedFinal, string forbiddenPrefix)
    {
        ExpectedFinal = expectedFinal;
        ForbiddenPrefix = forbiddenPrefix;
        lock (_writes)
            _writes.Clear();
    }

    public void RecordWrite(string value)
    {
        lock (_writes)
            _writes.Add(value);
    }

    public ProbeResult Evaluate()
    {
        string[] writes;
        lock (_writes)
            writes = _writes.ToArray();

        var final = writes.Length > 0 ? writes[^1] : null;
        var staleAfter = 0;
        var sawExpected = false;
        foreach (var w in writes)
        {
            if (string.Equals(w, ExpectedFinal, StringComparison.Ordinal))
                sawExpected = true;
            else if (sawExpected && ForbiddenPrefix is not null &&
                     w.StartsWith(ForbiddenPrefix, StringComparison.Ordinal))
                staleAfter++;
        }

        var okFinal = string.Equals(final, ExpectedFinal, StringComparison.Ordinal);
        var observed = $"writes={writes.Length}, final={final}, staleAfterRebind={staleAfter}";
        return new ProbeResult(
            Id,
            Name,
            "final value from new VM; no stale prefix writes after rebind",
            observed,
            okFinal && staleAfter == 0 ? ProbeVerdict.Pass : ProbeVerdict.Fail);
    }
}
