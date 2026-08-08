using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Interactivity;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>
/// Windows Gallery ViewModel — no Godot types; engine work flows through InteractionRequests.
/// </summary>
public partial class WindowsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _guideText =
        "Tap one step at a time. Overlays block content clicks; lab strip and nav stay visible.";

    [ObservableProperty]
    private string _stackText = "No overlay windows.";

    [ObservableProperty]
    private string _lastResultText = "No result yet";

    [ObservableProperty]
    private string _pageStatusText = "Running";

    public InteractionRequest OpenPopupRequest { get; } = new();
    public InteractionRequest OpenDialogRequest { get; } = new();
    public InteractionRequest OpenProgressRequest { get; } = new();
    public InteractionRequest EnqueueQueuedPopupsRequest { get; } = new();
    public InteractionRequest OpenNestedOverlaysRequest { get; } = new();
    public InteractionRequest OpenFullContrastRequest { get; } = new();
    public InteractionRequest FindDialogRequest { get; } = new();
    public InteractionRequest ClearWindowsRequest { get; } = new();

    [ObservableProperty]
    private string _pooledStatsText = "Pooled: live=0, created=0, reused=0";

    public InteractionRequest AllocatePooledRequest { get; } = new();
    public InteractionRequest FreePooledRequest { get; } = new();

    [RelayCommand]
    private void OpenPopup() => OpenPopupRequest.Raise();

    [RelayCommand]
    private void OpenDialog() => OpenDialogRequest.Raise();

    [RelayCommand]
    private void OpenProgress() => OpenProgressRequest.Raise();

    [RelayCommand]
    private void EnqueueQueuedPopups() => EnqueueQueuedPopupsRequest.Raise();

    [RelayCommand]
    private void OpenNestedOverlays() => OpenNestedOverlaysRequest.Raise();

    [RelayCommand]
    private void OpenFullContrast() => OpenFullContrastRequest.Raise();

    [RelayCommand]
    private void FindDialog() => FindDialogRequest.Raise();

    [RelayCommand]
    private void ClearWindows() => ClearWindowsRequest.Raise();

    [RelayCommand]
    private void AllocatePooled() => AllocatePooledRequest.Raise();

    [RelayCommand]
    private void FreePooled() => FreePooledRequest.Raise();

    public void SetGuide(string text) => GuideText = text;

    public void SetPageStatus(string text) => PageStatusText = text;

    public void UpdateStack(string dump) => StackText = dump;

    public void SetDialogResult(bool? result)
    {
        LastResultText = result switch
        {
            true => "OK",
            false => "Cancel",
            null => "No result yet"
        };
    }

    public void UpdatePooledStats(int liveCount, int createCount, int reuseCount)
        => PooledStatsText = $"Pooled: live={liveCount}, created={createCount}, reused={reuseCount}";
}
