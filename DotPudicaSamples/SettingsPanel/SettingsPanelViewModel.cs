using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;
using Godot;

namespace Samples.SettingsPanel;

public partial class SettingsPanelViewModel : ViewModelBase
{
    // Master volume (0~100)
    [ObservableProperty]
    private double _masterVolume = 80;

    // Whether background music is enabled
    [ObservableProperty]
    private bool _isMusicEnabled = true;

    // Quality level index (0=Low, 1=Medium, 2=High, 3=Ultra)
    [ObservableProperty]
    private int _qualityLevel = 2;

    // Volume string displayed to the user
    public string VolumeText => $"Volume: {(int)MasterVolume}";

    // When MasterVolume changes, sync refresh VolumeText
    partial void OnMasterVolumeChanged(double value)
        => OnPropertyChanged(nameof(VolumeText));

    [RelayCommand]
    private void Save()
    {
        // In a real project, this could be saved to a configuration file
        GD.Print($"[Settings] Saved - Volume: {MasterVolume}, Music: {IsMusicEnabled}, Quality: {QualityLevel}");
        Send(new DotPudica.Core.Messaging.NotificationMessage("SettingsSaved"));
    }
}
