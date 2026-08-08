using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Interactivity;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.ScopesAndDi;

/// <summary>
/// ScopesAndDi Gallery ViewModel — scope state and interaction requests.
/// View handles SceneContextHost lifecycle and reports back.
/// </summary>
public partial class ScopesAndDiViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText = "None";

    [ObservableProperty]
    private string _resultText = "—";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateScopeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DestroyScopeCommand))]
    private bool _scopeAlive;

    public InteractionRequest CreateScopeRequest { get; } = new();
    public InteractionRequest DestroyScopeRequest { get; } = new();

    private bool CanCreateScope() => !ScopeAlive;

    [RelayCommand(CanExecute = nameof(CanCreateScope))]
    private void CreateScope() => CreateScopeRequest.Raise();

    private bool CanDestroyScope() => ScopeAlive;

    [RelayCommand(CanExecute = nameof(CanDestroyScope))]
    private void DestroyScope() => DestroyScopeRequest.Raise();

    public void ReportScopeCreated(int instanceId, string greeting)
    {
        ScopeAlive = true;
        StatusText = "Alive";
        ResultText = $"#{instanceId} · {greeting}";
    }

    public void ReportScopeDestroyed(int? instanceId, bool isDisposed)
    {
        ScopeAlive = false;
        StatusText = "Destroyed";
        ResultText = instanceId is null
            ? "—"
            : $"#{instanceId} disposed={isDisposed}";
    }
}
