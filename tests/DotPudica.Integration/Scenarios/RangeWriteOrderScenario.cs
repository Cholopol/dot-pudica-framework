using DotPudica.Core.Binding;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Binding;
using DotPudica.Godot.Binding.ControlProxies;
using Godot;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DotPudica.Integration.Scenarios;

/// <summary>
/// Real ProgressBar: when Value is written before MaxValue, raising Max should restore the expected Value (GodotRangeBinding).
/// </summary>
public sealed class RangeWriteOrderScenario : IIntegrationScenario
{
    public string Name => "Range_ValueBeforeMax_RetainsDesiredOnProgressBar";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);

        var bar = new ProgressBar
        {
            Name = "RangeBar",
            MinValue = 0,
            MaxValue = 100,
            Value = 0
        };
        host.AddChild(bar);

        var valueProxy = new ProgressBarProxy(bar, RangeBindingProperty.Value);
        var maxProxy = new ProgressBarProxy(bar, RangeBindingProperty.MaxValue);
        var context = new BindingContext();
        context.SetUiDispatcher(dispatcher);

        var vm = new RangeFixtureViewModel { Hp = 130, MaxHp = 100 };
        var valuePath = new TypedBindingPath<RangeFixtureViewModel, double>(
            static x => x.Hp, static (x, v) => x.Hp = v, ["Hp"]);
        var maxPath = new TypedBindingPath<RangeFixtureViewModel, double>(
            static x => x.MaxHp, static (x, v) => x.MaxHp = v, ["MaxHp"]);

        context.AddBinding(new PropertyBinding<double, double>(valueProxy, valuePath, BindingMode.OneWay, dispatcher: dispatcher));
        context.AddBinding(new PropertyBinding<double, double>(maxProxy, maxPath, BindingMode.OneWay, dispatcher: dispatcher));
        context.DataContext = vm;
        await IntegrationTestHelpers.WaitProcessFrame(host);

        try
        {
            vm.Hp = 130;
            await IntegrationTestHelpers.WaitFrames(host, 1);
            vm.MaxHp = 130;
            await IntegrationTestHelpers.WaitFrames(host, 2);

            if (!Mathf.IsEqualApprox(bar.MaxValue, 130))
                return IntegrationResult.Fail(Name, $"MaxValue={bar.MaxValue}, expected 130");

            if (!Mathf.IsEqualApprox(bar.Value, 130))
                return IntegrationResult.Fail(Name,
                    $"Value was clamped/lost: Value={bar.Value}, expected 130 (Max={bar.MaxValue})");

            return IntegrationResult.Pass(Name);
        }
        finally
        {
            context.Dispose();
            bar.QueueFree();
        }
    }
}

public partial class RangeFixtureViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _hp;

    [ObservableProperty]
    private double _maxHp = 100;
}
