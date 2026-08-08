using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;

namespace Samples.Showcase.Gallery.Commands;

/// <summary>
/// Commands demo: RelayCommand, CanExecute gating, AsyncRelayCommand, parameterized commands.
/// </summary>
public partial class CommandsViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _clickCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunLockedCommand))]
    private bool _isUnlocked;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadDataCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _resultText = "Not loaded yet.";

    [ObservableProperty]
    private int _currentLevel = 1;

    [ObservableProperty]
    private string _lastCommandLog = "None";

    /// <summary>Fixed parameter for [BindCommand(Parameter = ...)] on level buttons.</summary>
    public int LevelOptionA => 1;

    public int LevelOptionB => 2;

    public int LevelOptionC => 3;

    [RelayCommand]
    private void Increment()
    {
        ClickCount++;
        LastCommandLog = $"Increment → {ClickCount}";
    }

    private bool CanRunLocked() => IsUnlocked;

    [RelayCommand(CanExecute = nameof(CanRunLocked))]
    private void RunLocked()
    {
        LastCommandLog = "RunLocked succeeded (unlocked)";
    }

    private bool CanLoad() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ResultText = "Loading…";
        LastCommandLog = "LoadData started";

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1.2));
            ResultText = $"Loaded @ {DateTime.Now:HH:mm:ss}";
            LastCommandLog = "LoadData completed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetLevel(int level)
    {
        CurrentLevel = level;
        LastCommandLog = $"SetLevel({level})";
    }
}
