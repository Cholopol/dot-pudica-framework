using DotPudica.Core.Binding;
using DotPudica.Godot.Binding.ControlProxies;
using Godot;
using DotPudica.Integration.Controls;
using DotPudica.Integration.Fixtures;

namespace DotPudica.Integration.Scenarios;

/// <summary>After QueueFree / _ExitTree disposes bindings, modifying the old ViewModel must not reach the proxy.</summary>
public sealed class LifecycleScenario : IIntegrationScenario
{
    public string Name => "ExitTree_DisposesBindings";

    public async Task<IntegrationResult> RunAsync(Node host)
    {
        var syncContext = Dispatcher.SynchronizationContext
            ?? throw new InvalidOperationException("Missing Godot SynchronizationContext");
        var dispatcher = UiDispatcher.FromSynchronizationContext(syncContext);

        var holder = new BindingHostControl { Name = "LifecycleHolder" };
        host.AddChild(holder);

        var label = new Label { Name = "LifecycleLabel", Text = "" };
        holder.AddChild(label);

        var accessLog = new TargetAccessLog();
        var proxy = new LoggingTargetProxy(new LabelProxy(label), accessLog);
        var context = new BindingContext();
        context.SetUiDispatcher(dispatcher);
        holder.Attach(context);

        var vm = new IntegrationTitleViewModel { Title = "alive" };
        var path = new TypedBindingPath<IntegrationTitleViewModel, string>(
            static x => x.Title,
            static (x, v) => x.Title = v,
            ["Title"]);
        context.AddBinding(new PropertyBinding(proxy, path, BindingMode.OneWay, dispatcher: dispatcher));
        context.DataContext = vm;
        await IntegrationTestHelpers.WaitProcessFrame(host);

        if (label.Text != "alive")
            return IntegrationResult.Fail(Name, $"Initial binding failed, Text={label.Text}");

        if (accessLog.AccessCount == 0)
            return IntegrationResult.Fail(Name, "No proxy access observed during initial binding");

        var accessBeforeDispose = accessLog.AccessCount;
        holder.QueueFree();
        await IntegrationTestHelpers.WaitFrames(host, 2);

        vm.Title = "after-exit";
        await IntegrationTestHelpers.WaitFrames(host, 2);

        if (accessLog.AccessCount != accessBeforeDispose)
            return IntegrationResult.Fail(Name,
                $"Target access still occurred after ExitTree: before={accessBeforeDispose}, after={accessLog.AccessCount}");

        return IntegrationResult.Pass(Name);
    }
}
