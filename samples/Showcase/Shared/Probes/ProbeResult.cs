namespace Samples.Showcase.Shared.Probes;

public enum ProbeVerdict
{
    Pass,
    Fail,
    Evidence
}

public sealed record ProbeResult(
    string Id,
    string Name,
    string Expectation,
    string Observed,
    ProbeVerdict Verdict)
{
    public bool Passed => Verdict is ProbeVerdict.Pass or ProbeVerdict.Evidence;
}

/// <summary>Reusable pure-logic probe contract; shared by Gallery and Integration Scenario.</summary>
public interface IProbe
{
    string Id { get; }
    string Name { get; }
}
