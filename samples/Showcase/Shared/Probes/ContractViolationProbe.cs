namespace Samples.Showcase.Shared.Probes;

/// <summary>G: Contract violations — first two items expect exceptions; third item (background collection mutation) recorded as a known gap.</summary>
public sealed class ContractViolationProbe : IProbe
{
    public string Id => "G";
    public string Name => "Contract violations";

    public bool TwoWayOffThreadThrew { get; private set; }
    public string? TwoWayExceptionType { get; private set; }
    public bool LifecycleOffThreadThrew { get; private set; }
    public string? LifecycleExceptionType { get; private set; }
    public string? CollectionMutationObservation { get; private set; }
    public bool CollectionMutationIsKnownGap { get; private set; } = true;

    public void Reset()
    {
        TwoWayOffThreadThrew = false;
        TwoWayExceptionType = null;
        LifecycleOffThreadThrew = false;
        LifecycleExceptionType = null;
        CollectionMutationObservation = null;
    }

    public void RecordTwoWay(Exception? ex)
    {
        TwoWayOffThreadThrew = ex is not null;
        TwoWayExceptionType = ex?.GetType().Name;
    }

    public void RecordLifecycle(Exception? ex)
    {
        LifecycleOffThreadThrew = ex is not null;
        LifecycleExceptionType = ex?.GetType().Name;
    }

    public void RecordCollectionMutation(string observation)
        => CollectionMutationObservation = observation;

    public ProbeResult Evaluate()
    {
        var ok = TwoWayOffThreadThrew && LifecycleOffThreadThrew;
        var observed =
            $"twoWayThrew={TwoWayOffThreadThrew}({TwoWayExceptionType}), lifecycleThrew={LifecycleOffThreadThrew}({LifecycleExceptionType}), collection={CollectionMutationObservation ?? "n/a"} [KNOWN_GAP={CollectionMutationIsKnownGap}]";
        return new ProbeResult(
            Id,
            Name,
            "TwoWay/lifecycle throw off UI thread; collection mutation is a known gap",
            observed,
            ok ? ProbeVerdict.Pass : ProbeVerdict.Fail);
    }
}
