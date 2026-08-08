using DotPudica.Core.ViewModels;
using DotPudica.Core.Binding;
using DotPudica.Godot.Views;
using Godot;
using Samples.Showcase.Shared.Probes;

namespace DotPudica.Integration.Scenarios;

/// <summary>Contract violation: TwoWay target changes and lifecycle operations should throw on non-UI threads (first two items of probe G).</summary>
public sealed class ContractViolationScenario : IIntegrationScenario
{
    public string Name => "ContractViolation_ThrowsOffUiThread";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var probe = new ContractViolationProbe();
        probe.Reset();

        using var runtime = new DotPudicaViewRuntime<ProbeTitleViewModel>();
        runtime.CaptureUiContext();
        var vm = new ProbeTitleViewModel { Title = "vm" };
        runtime.SetViewModel(vm, ViewModelOwnership.Owned);

        var path = new TypedBindingPath<ProbeTitleViewModel, string>(
            static x => x.Title, static (x, v) => x.Title = v, ["Title"]);
        var proxy = new RecordingStringProxy();
        runtime.BindProperty(proxy, path, BindingMode.TwoWay);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        Exception? twoWayEx = null;
        await Task.Run(() =>
        {
            try { proxy.RaiseValueChanged(); }
            catch (Exception ex) { twoWayEx = ex; }
        });
        probe.RecordTwoWay(twoWayEx);

        Exception? lifecycleEx = null;
        await Task.Run(() =>
        {
            try { runtime.SetViewModel(new ProbeTitleViewModel { Title = "other" }, ViewModelOwnership.Owned); }
            catch (Exception ex) { lifecycleEx = ex; }
        });
        probe.RecordLifecycle(lifecycleEx);

        // Third item: record known gap (not an additional assertion beyond the FAIL condition)
        probe.RecordCollectionMutation("integration-skip-collection-gap");

        var result = probe.Evaluate();
        return result.Verdict == ProbeVerdict.Pass
            ? IntegrationResult.Pass(Name)
            : IntegrationResult.Fail(Name, result.Observed);
    }
}
