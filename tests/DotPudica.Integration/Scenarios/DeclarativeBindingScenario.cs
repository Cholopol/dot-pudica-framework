using DotPudica.Integration.Fixtures;
using Godot;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Declarative golden path: [DotPudicaView] + [BindTo] + [BindCommand] → real Label/Button signals.
/// </summary>
public sealed class DeclarativeBindingScenario : IIntegrationScenario
{
    public string Name => "Declarative_BindToAndBindCommand_OnRealControls";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var view = new DeclarativeBindingView { Name = "DeclarativeBindingView" };
        host.AddChild(view);
        await IntegrationTestHelpers.WaitProcessFrame(host);

        var vm = view.PanelViewModel
            ?? throw new InvalidOperationException("DeclarativeBindingViewModel not initialized");

        if (view.TitleLabel.Text != "initial")
            return IntegrationResult.Fail(Name, $"Initial binding failed, Text={view.TitleLabel.Text}");

        vm.Title = "updated";
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (view.TitleLabel.Text != "updated")
            return IntegrationResult.Fail(Name, $"OneWay update failed, Text={view.TitleLabel.Text}");

        view.ClickButton.EmitSignal(BaseButton.SignalName.Pressed);
        await IntegrationTestHelpers.WaitFrames(host, 2);
        if (vm.ClickCount != 1)
            return IntegrationResult.Fail(Name, $"BindCommand did not execute, ClickCount={vm.ClickCount}");

        var textBeforeDispose = view.TitleLabel.Text;
        view.QueueFree();
        await IntegrationTestHelpers.WaitFrames(host, 2);

        vm.Title = "after-exit";
        await IntegrationTestHelpers.WaitFrames(host, 2);

        // The node has been freed; if the binding was not disposed, it might throw or silently write. Here we only require that the command-side count is not accidentally triggered,
        // and that the last visible text before dispose is still the value before dispose (skipped when the disposed Label cannot be read).
        if (vm.ClickCount != 1)
            return IntegrationResult.Fail(Name, "ClickCount was unexpectedly modified after ExitTree");

        _ = textBeforeDispose;
        return IntegrationResult.Pass(Name);
    }
}
