using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.ViewModels;
using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.MiniGame.Battle;

/// <summary>
/// Battle ViewModel: combat stats from loadout snapshot; main HUD + mini bar External-bind this instance.
/// </summary>
public partial class BattleViewModel : ViewModelBase
{
    private readonly PlayerCombatStats _loadout;
    private readonly Random _rng = new();
    private double _elapsedSeconds;
    private double _enemyAttackCooldown;
    private bool _finished;
    private bool _defending;
    private double _defendRemaining;

    public BattleViewModel(PlayerCombatStats loadout)
    {
        _loadout = loadout;
        MaxHp = Math.Max(1, loadout.MaxHp);
        Hp = MaxHp;
        EnergyMax = Math.Max(1, loadout.EnergyMax);
        Energy = EnergyMax;
        Attack = loadout.Attack;
        Defense = loadout.Defense;
        PowerTotal = loadout.PowerTotal;
        SpawnEnemy();
        AppendLog($"Deployed ATK{Attack}/DEF{Defense}/HP{MaxHp}/EN{EnergyMax} (Power {PowerTotal})");
    }

    public ObservableCollection<BattleLogEntry> LogEntries { get; } = new();

    [ObservableProperty] private int _hp;
    [ObservableProperty] private int _maxHp;
    [ObservableProperty] private float _energy;
    [ObservableProperty] private float _energyMax;
    [ObservableProperty] private int _score;
    [ObservableProperty] private long _tick;
    [ObservableProperty] private int _attack;
    [ObservableProperty] private int _defense;
    [ObservableProperty] private int _powerTotal;
    [ObservableProperty] private int _killCount;
    [ObservableProperty] private int _enemyHp;
    [ObservableProperty] private int _enemyMaxHp;
    [ObservableProperty] private string _enemyName = "";
    [ObservableProperty] private string _phaseText = "In combat";
    [ObservableProperty] private string _loadoutSummary = "";
    [ObservableProperty] private bool _skillReady = true;
    [ObservableProperty] private float _hpRatio = 1f;
    [ObservableProperty] private float _energyRatio = 1f;
    [ObservableProperty] private float _enemyHpRatio = 1f;

    public event Action<BattleResult>? BattleFinished;

    public void Advance(double deltaSeconds)
    {
        if (IsDisposed || _finished)
            return;

        _elapsedSeconds += deltaSeconds;
        Tick++;

        if (_defending)
        {
            _defendRemaining -= deltaSeconds;
            if (_defendRemaining <= 0)
            {
                _defending = false;
                PhaseText = "In combat";
            }
        }

        Energy = (float)Math.Clamp(Energy + deltaSeconds * 8, 0, EnergyMax);
        var ready = Energy >= 25f;
        if (ready != SkillReady)
        {
            SkillReady = ready;
            FireSkillCommand.NotifyCanExecuteChanged();
        }
        else
        {
            SkillReady = ready;
        }
        RefreshRatios();

        _enemyAttackCooldown -= deltaSeconds;
        if (_enemyAttackCooldown <= 0)
        {
            EnemySwing();
            _enemyAttackCooldown = 1.35;
        }

        if (Hp <= 0)
            Finish(won: false);
        else if (_elapsedSeconds >= 20.0)
            Finish(won: KillCount > 0);
    }

    [RelayCommand(CanExecute = nameof(CanFireSkill))]
    private void FireSkill()
    {
        if (_finished || Energy < 25f)
            return;

        Energy -= 25f;
        var damage = Math.Max(1, Attack + _rng.Next(0, 8));
        EnemyHp = Math.Max(0, EnemyHp - damage);
        Score += damage;
        AppendLog($"Skill hit {EnemyName} -{damage}");
        SkillReady = Energy >= 25f;
        RefreshRatios();
        NotifyCanExecute();

        if (EnemyHp <= 0)
            OnEnemyDown();
    }

    private bool CanFireSkill() => !_finished && Energy >= 25f;

    [RelayCommand]
    private void Defend()
    {
        if (_finished)
            return;

        _defending = true;
        _defendRemaining = 1.2;
        PhaseText = "Defending";
        AppendLog("Entered defend");
    }

    [RelayCommand]
    private void BasicAttack()
    {
        if (_finished)
            return;

        var damage = Math.Max(1, Attack / 2 + _rng.Next(0, 5));
        EnemyHp = Math.Max(0, EnemyHp - damage);
        Score += damage / 2;
        AppendLog($"Basic attack {EnemyName} -{damage}");
        RefreshRatios();

        if (EnemyHp <= 0)
            OnEnemyDown();
    }

    [RelayCommand]
    private void Flee()
    {
        if (_finished)
            return;
        AppendLog("Fled");
        Finish(won: false);
    }

    private void EnemySwing()
    {
        if (_finished)
            return;

        var raw = 8 + KillCount * 2 + _rng.Next(0, 10);
        var mitigated = Math.Max(1, raw - Defense / 4);
        if (_defending)
            mitigated = Math.Max(1, mitigated / 2);

        Hp = Math.Max(0, Hp - mitigated);
        AppendLog($"{EnemyName} strikes -{mitigated}");
        RefreshRatios();
    }

    private void OnEnemyDown()
    {
        KillCount++;
        Score += 50 + Attack;
        AppendLog($"Downed {EnemyName} (kills {KillCount})");
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        var tier = KillCount + 1;
        EnemyName = $"Intruder-{tier}";
        EnemyMaxHp = 40 + tier * 18 + _rng.Next(0, 12);
        EnemyHp = EnemyMaxHp;
        _enemyAttackCooldown = 1.0;
        RefreshRatios();
    }

    private void Finish(bool won)
    {
        if (_finished)
            return;

        _finished = true;
        PhaseText = won ? "Victory" : "Defeat";
        NotifyCanExecute();
        BattleFinished?.Invoke(new BattleResult(
            Score,
            won,
            TimeSpan.FromSeconds(_elapsedSeconds),
            KillCount,
            _loadout));
    }

    private void AppendLog(string text)
    {
        LogEntries.Insert(0, new BattleLogEntry(text, Tick));
        while (LogEntries.Count > 40)
            LogEntries.RemoveAt(LogEntries.Count - 1);
    }

    private void RefreshRatios()
    {
        HpRatio = MaxHp <= 0 ? 0 : (float)Hp / MaxHp;
        EnergyRatio = EnergyMax <= 0 ? 0 : Energy / EnergyMax;
        EnemyHpRatio = EnemyMaxHp <= 0 ? 0 : (float)EnemyHp / EnemyMaxHp;
        LoadoutSummary = $"Loadout ATK{Attack} DEF{Defense} Power{PowerTotal}";
    }

    private void NotifyCanExecute() => FireSkillCommand.NotifyCanExecuteChanged();

    partial void OnEnergyChanged(float value) => SkillReady = !_finished && value >= 25f;
}
