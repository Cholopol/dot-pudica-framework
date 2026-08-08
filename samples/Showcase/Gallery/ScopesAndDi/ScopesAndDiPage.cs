using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Godot;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.ScopesAndDi;

/// <summary>
/// ScopesAndDi — SceneContextHost creates/disposes a transient ViewModel scope.
/// </summary>
[DotPudicaView(typeof(ScopesAndDiViewModel))]
public partial class ScopesAndDiPage : ShowcasePageWindow
{
    private SceneContextHost? _contextHost;
    private InjectedDemoViewModel? _vm;

    [Export, BindTo(nameof(ScopesAndDiViewModel.StatusText))]
    private Label _statusLabel = null!;

    [Export, BindTo(nameof(ScopesAndDiViewModel.ResultText))]
    private Label _resultLabel = null!;

    [Export, BindCommand(nameof(ScopesAndDiViewModel.CreateScopeCommand))]
    private Button _createButton = null!;

    [Export, BindCommand(nameof(ScopesAndDiViewModel.DestroyScopeCommand))]
    private Button _destroyButton = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    partial void OnViewDisposing()
    {
        _vm?.Dispose();
        _vm = null;
    }

    [Subscribe("CreateScopeRequest.Raised")]
    private void OnCreateScopeRequested(object? sender, EventArgs e)
    {
        if (_contextHost is not null)
            return;

        _contextHost = new SceneContextHost { Name = "DemoSceneContextHost" };
        AddChild(_contextHost);

        _vm = _contextHost.Scope.ViewModels.Create<InjectedDemoViewModel>();
        var userName = _vm.ProfileService.CurrentUserName ?? "(anonymous)";
        _vm.Greeting = $"Hello from scope · user={userName}";

        ViewModel!.ReportScopeCreated(_vm.InstanceId, _vm.Greeting);
    }

    [Subscribe("DestroyScopeRequest.Raised")]
    private void OnDestroyScopeRequested(object? sender, EventArgs e)
    {
        if (_contextHost is null)
            return;

        RemoveChild(_contextHost);
        _contextHost.QueueFree();
        _contextHost = null;

        var instanceId = _vm?.InstanceId;
        _vm?.Dispose();
        var isDisposed = _vm?.IsDisposed ?? false;
        _vm = null;

        ViewModel!.ReportScopeDestroyed(instanceId, isDisposed);
    }

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "SceneContextHost creates and disposes transient VMs.");

        var actions = ShowcaseUi.AddActionRow(root);
        _createButton = ShowcaseUi.CreatePrimaryButton("Create Scope");
        _destroyButton = ShowcaseUi.CreateActionButton("Destroy Scope");
        actions.AddChild(_createButton);
        actions.AddChild(_destroyButton);

        var metrics = ShowcaseUi.AddMetricsRow(root);
        ShowcaseUi.AddMetricChip(metrics, "Status", out _statusLabel);
        ShowcaseUi.AddMetricChip(metrics, "Result", out _resultLabel);
    }
}
