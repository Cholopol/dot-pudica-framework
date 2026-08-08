namespace Samples.Showcase.Shared.Probes;

/// <summary>B: Coalesce — final state preserved; target writes ≤ maxWrites and final value is correct.</summary>
public sealed class CoalesceProbe : IProbe
{
    public string Id => "B";
    public string Name => "Coalesce final state";

    public int PropertyChangedCount { get; private set; }
    public int TargetWriteCount { get; private set; }
    public string? FinalValue { get; private set; }
    public string? ExpectedFinal { get; private set; }
    public int MaxAllowedWrites { get; set; } = 2;

    public void Reset(string expectedFinal)
    {
        PropertyChangedCount = 0;
        TargetWriteCount = 0;
        FinalValue = null;
        ExpectedFinal = expectedFinal;
    }

    public void OnPropertyChanged() => PropertyChangedCount++;

    public void OnTargetWrite(string value)
    {
        TargetWriteCount++;
        FinalValue = value;
    }

    public ProbeResult Evaluate()
    {
        var okWrites = TargetWriteCount > 0 && TargetWriteCount <= MaxAllowedWrites;
        var okFinal = string.Equals(FinalValue, ExpectedFinal, StringComparison.Ordinal);
        var observed =
            $"PropertyChanged={PropertyChangedCount}, writes={TargetWriteCount}, final={FinalValue}, expected={ExpectedFinal}";
        return new ProbeResult(
            Id,
            Name,
            $"writes in [1,{MaxAllowedWrites}] and final == expected",
            observed,
            okWrites && okFinal ? ProbeVerdict.Pass : ProbeVerdict.Fail);
    }
}
