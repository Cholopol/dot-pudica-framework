namespace Samples.Showcase.Shared.Probes;

/// <summary>F: Scene churn — zero writes after exit.</summary>
public sealed class CancelLifecycleProbe : IProbe
{
    public string Id => "F";
    public string Name => "Cancel lifecycle";

    private int _resultsAfterExit;

    public int EnterCount { get; private set; }
    public int ExitCount { get; private set; }
    public int ResultsAfterExit => Volatile.Read(ref _resultsAfterExit);
    public int Exceptions { get; private set; }
    public int ActiveCtsAtEnd { get; set; }

    public void Reset()
    {
        EnterCount = 0;
        ExitCount = 0;
        Volatile.Write(ref _resultsAfterExit, 0);
        Exceptions = 0;
        ActiveCtsAtEnd = 0;
    }

    public void OnEnter() => EnterCount++;
    public void OnExit() => ExitCount++;
    public void OnResultAfterExit() => Interlocked.Increment(ref _resultsAfterExit);
    public void OnException() => Exceptions++;

    public ProbeResult Evaluate()
    {
        var ok = EnterCount == ExitCount
                 && EnterCount > 0
                 && ResultsAfterExit == 0
                 && Exceptions == 0
                 && ActiveCtsAtEnd == 0;
        var observed =
            $"enter={EnterCount}, exit={ExitCount}, resultsAfterExit={ResultsAfterExit}, exceptions={Exceptions}, activeCts={ActiveCtsAtEnd}";
        return new ProbeResult(
            Id,
            Name,
            "enter == exit; zero writes after exit; no exceptions; CTS == 0",
            observed,
            ok ? ProbeVerdict.Pass : ProbeVerdict.Fail);
    }
}
