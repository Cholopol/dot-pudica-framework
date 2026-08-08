using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.BindingBasics;

/// <summary>
/// Binding demo: OneWay / TwoWay / OneWayToSource / OneTime modes
/// and built-in DotPudica.Core.Binding.Converters.
/// </summary>
public partial class BindingBasicsViewModel : ViewModelBase
{
    private readonly Random _random = new();

    [ObservableProperty]
    private int _counter;

    [ObservableProperty]
    private string _userName = "";

    [ObservableProperty]
    private string _rawInput = "";

    [ObservableProperty]
    private int _initialSeed;

    [ObservableProperty]
    private bool _isFeatureEnabled = true;

    [ObservableProperty]
    private bool _showDetails = true;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private float _progressRatio;

    [ObservableProperty]
    private object _lastAction = "No action yet";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectionCleared))]
    private object? _selectedOption;

    public bool IsSelectionCleared => SelectedOption is null;

    public BindingBasicsViewModel()
    {
        InitialSeed = _random.Next(1000, 9999);
    }

    [RelayCommand]
    private void Increment()
    {
        Counter++;
        LastAction = $"Counter += 1 → {Counter}";
    }

    [RelayCommand]
    private void OverwriteUserNameFromVm()
    {
        UserName = $"Player{_random.Next(10, 99)}";
        LastAction = $"UserName ← VM \"{UserName}\" (TwoWay input syncs)";
    }

    [RelayCommand]
    private void RegenerateSeed()
    {
        InitialSeed = _random.Next(1000, 9999);
        LastAction = $"Regenerated seed → {InitialSeed} (OneTime label stays frozen)";
    }

    [RelayCommand]
    private void OverwriteRawInputFromVm()
    {
        RawInput = $"VM-write-{_random.Next(100, 999)}";
        LastAction = $"RawInput ← VM \"{RawInput}\" (OneWayToSource field unchanged)";
    }

    [RelayCommand]
    private void BumpProgress()
    {
        ProgressRatio = ProgressRatio >= 1f ? 0f : ProgressRatio + 0.25f;
        LastAction = $"Progress → {ProgressRatio:F2}";
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedOption = null;
        LastAction = "Selection cleared";
    }

    [RelayCommand]
    private void SelectOptionA()
    {
        SelectedOption = "A";
        LastAction = "Selected A";
    }

    [RelayCommand]
    private void SelectOptionB()
    {
        SelectedOption = "B";
        LastAction = "Selected B";
    }
}
