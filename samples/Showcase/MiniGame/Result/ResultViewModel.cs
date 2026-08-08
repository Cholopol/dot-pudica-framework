using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;
using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.MiniGame.Result;

/// <summary>Result VM texts — values only; chip keys supply the labels.</summary>
public partial class ResultViewModel : ViewModelBase
{
    public ResultViewModel(BattleResult result)
    {
        Result = result;
        TitleText = result.Won ? "Victory" : "Defeat";
        ScoreText = $"{result.FinalScore} · {result.KillCount} kills";
        DurationText = $"{result.Duration.TotalSeconds:F1}s";
        LoadoutText = result.LoadoutStats is { } s
            ? $"ATK {s.Attack} · DEF {s.Defense} · HP {s.MaxHp} · EN {s.EnergyMax} · PWR {s.PowerTotal}"
            : "No snapshot";
    }

    public BattleResult Result { get; }

    [ObservableProperty]
    private string _titleText = "";

    [ObservableProperty]
    private string _scoreText = "";

    [ObservableProperty]
    private string _durationText = "";

    [ObservableProperty]
    private string _loadoutText = "";

    public event Action? BackToLobbyRequested;
    public event Action? BackToLoginRequested;

    [RelayCommand]
    private void BackToLobby() => BackToLobbyRequested?.Invoke();

    [RelayCommand]
    private void BackToLogin() => BackToLoginRequested?.Invoke();
}
