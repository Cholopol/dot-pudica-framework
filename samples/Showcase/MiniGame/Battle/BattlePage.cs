using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.Binding.Converters;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;
using Samples.Showcase.Shared.Models;

namespace Samples.Showcase.MiniGame.Battle;

/// <summary>Mini HUD bar: shares BattleViewModel with the main stage (External).</summary>
[DotPudicaView(typeof(BattleViewModel), AutoInitialize = false)]
public partial class BattleMiniHud : HBoxContainer
{
    [Export, BindTo(nameof(BattleViewModel.PhaseText))]
    private Label _phaseLabel = null!;

    [Export, BindTo(nameof(BattleViewModel.Hp))]
    private ProgressBar _hpBar = null!;

    [BindTo(nameof(BattleViewModel.MaxHp), Target = nameof(ProgressBar.MaxValue))]
    private ProgressBar _hpBarMaxHp = null!;

    [Export, BindTo(nameof(BattleViewModel.Energy))]
    private ProgressBar _energyBar = null!;

    [BindTo(nameof(BattleViewModel.EnergyMax), Target = nameof(ProgressBar.MaxValue))]
    private ProgressBar _energyBarMaxEnergy = null!;

    [Export, BindTo(nameof(BattleViewModel.Score), Converter = typeof(IntToStringConverter))]
    private Label _scoreLabel = null!;

    [Export, BindTo(nameof(BattleViewModel.KillCount), Converter = typeof(IntToStringConverter))]
    private Label _killLabel = null!;

    public BattleMiniHud()
    {
        Name = "MiniHud";
        AddThemeConstantOverride("separation", 12);

        AddChild(new Label { Text = "Phase", Modulate = ShowcaseTheme.Muted });
        _phaseLabel = new Label { Text = "In combat", CustomMinimumSize = new Vector2(80, 0) };
        AddChild(_phaseLabel);

        AddChild(new Label { Text = "HP", Modulate = ShowcaseTheme.Muted });
        _hpBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(120, 16),
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        AddChild(_hpBar);
        _hpBarMaxHp = _hpBar;

        AddChild(new Label { Text = "EN", Modulate = ShowcaseTheme.Muted });
        _energyBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(100, 16),
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        AddChild(_energyBar);
        _energyBarMaxEnergy = _energyBar;

        AddChild(new Label { Text = "Score", Modulate = ShowcaseTheme.Muted });
        _scoreLabel = new Label { Text = "0" };
        AddChild(_scoreLabel);

        AddChild(new Label { Text = "Kills", Modulate = ShowcaseTheme.Muted });
        _killLabel = new Label { Text = "0" };
        AddChild(_killLabel);
    }

    public override void _ExitTree() => DisposeView();

    public void BindShared(BattleViewModel shared)
    {
        SetViewModel(shared, ViewModelOwnership.External);
        DotPudicaInitialize();
    }
}

/// <summary>Main battle stage: enemy, actions, log. Shares BattleViewModel.</summary>
[DotPudicaView(typeof(BattleViewModel), AutoInitialize = false)]
public partial class BattleMainHud : VBoxContainer
{
    private const string LogItemScenePath = "res://samples/Showcase/MiniGame/Battle/BattleLogItem.tscn";

    [Export, BindTo(nameof(BattleViewModel.LoadoutSummary))]
    private Label _loadoutLabel = null!;

    [Export, BindTo(nameof(BattleViewModel.EnemyName))]
    private Label _enemyNameLabel = null!;

    [Export, BindTo(nameof(BattleViewModel.EnemyHp))]
    private ProgressBar _enemyHpBar = null!;

    [BindTo(nameof(BattleViewModel.EnemyMaxHp), Target = nameof(ProgressBar.MaxValue))]
    private ProgressBar _enemyHpBarMaxHp = null!;

    [Export, BindTo(nameof(BattleViewModel.Hp))]
    private ProgressBar _hpBar = null!;

    [BindTo(nameof(BattleViewModel.MaxHp), Target = nameof(ProgressBar.MaxValue))]
    private ProgressBar _hpBarMaxHp = null!;

    [Export, BindTo(nameof(BattleViewModel.Energy))]
    private ProgressBar _energyBar = null!;

    [BindTo(nameof(BattleViewModel.EnergyMax), Target = nameof(ProgressBar.MaxValue))]
    private ProgressBar _energyBarMaxEnergy = null!;

    [Export, BindTo(nameof(BattleViewModel.Attack), Converter = typeof(IntToStringConverter))]
    private Label _attackLabel = null!;

    [Export, BindTo(nameof(BattleViewModel.Defense), Converter = typeof(IntToStringConverter))]
    private Label _defenseLabel = null!;

    [Export, BindCommand(nameof(BattleViewModel.BasicAttackCommand))]
    private Button _basicAttackButton = null!;

    [Export, BindCommand(nameof(BattleViewModel.FireSkillCommand))]
    private Button _skillButton = null!;

    [Export, BindCommand(nameof(BattleViewModel.DefendCommand))]
    private Button _defendButton = null!;

    [Export, BindCommand(nameof(BattleViewModel.FleeCommand))]
    private Button _fleeButton = null!;

    [Export, ItemsSource(nameof(BattleViewModel.LogEntries), LogItemScenePath, PoolSize = 24)]
    private VBoxContainer _logList = null!;

    public BattleMainHud()
    {
        Name = "MainHud";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", ShowcaseTheme.Separation);

        _loadoutLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        AddChild(_loadoutLabel);

        ShowcaseUi.AddSection(this, "Enemy");
        _enemyNameLabel = new Label();
        AddChild(_enemyNameLabel);
        _enemyHpBar = new ProgressBar { MinValue = 0, MaxValue = 100, ShowPercentage = true };
        AddChild(_enemyHpBar);
        _enemyHpBarMaxHp = _enemyHpBar;

        ShowcaseUi.AddSection(this, "Player");
        AddChild(new Label { Text = "HP", Modulate = ShowcaseTheme.Muted });
        _hpBar = new ProgressBar { MinValue = 0, MaxValue = 100, ShowPercentage = true };
        AddChild(_hpBar);
        _hpBarMaxHp = _hpBar;

        AddChild(new Label { Text = "Energy", Modulate = ShowcaseTheme.Muted });
        _energyBar = new ProgressBar { MinValue = 0, MaxValue = 100, ShowPercentage = false };
        AddChild(_energyBar);
        _energyBarMaxEnergy = _energyBar;

        var stats = ShowcaseUi.AddMetricsRow(this);
        ShowcaseUi.AddMetricChip(stats, "ATK", out _attackLabel);
        ShowcaseUi.AddMetricChip(stats, "DEF", out _defenseLabel);

        var actions = ShowcaseUi.AddActionRow(this);
        _basicAttackButton = ShowcaseUi.CreatePrimaryButton("Basic Attack");
        _skillButton = ShowcaseUi.CreateActionButton("Skill (25 EN)");
        _defendButton = ShowcaseUi.CreateActionButton("Defend");
        _fleeButton = ShowcaseUi.CreateActionButton("Flee");
        actions.AddChild(_basicAttackButton);
        actions.AddChild(_skillButton);
        actions.AddChild(_defendButton);
        actions.AddChild(_fleeButton);

        ShowcaseUi.AddSection(this, "Combat Log");
        var logScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 140)
        };
        AddChild(logScroll);
        _logList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        logScroll.AddChild(_logList);
    }

    public override void _ExitTree() => DisposeView();

    public void BindShared(BattleViewModel shared)
    {
        SetViewModel(shared, ViewModelOwnership.External);
        DotPudicaInitialize();
    }
}

/// <summary>Battle page: owns BattleViewModel; mini HUD + main stage share External binding.</summary>
[DotPudicaView(typeof(BattleViewModel))]
public partial class BattlePage : ShowcasePageWindow
{
    private BattleMiniHud _miniHud = null!;
    private BattleMainHud _mainHud = null!;
    private PlayerCombatStats _stats = new(10, 5, 100, 100, 30);

    public void SetCombatStats(PlayerCombatStats stats) => _stats = stats;

    [ViewModelFactory]
    private BattleViewModel CreateBattleViewModel() => new(_stats);

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    partial void OnViewModelBound()
    {
        _miniHud.BindShared(ViewModel!);
        _mainHud.BindShared(ViewModel!);
    }

    public override void _PhysicsProcess(double delta) => ViewModel?.Advance(delta);

    [Subscribe(nameof(BattleViewModel.BattleFinished))]
    private void OnBattleFinished(BattleResult result)
    {
        var resultPage = new Result.ResultPage { WindowName = "Result" };
        resultPage.SetBattleResult(result);
        ShowFullPage(resultPage);
        RequireWindowManager().Dismiss(this);
    }

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "Mini HUD + main stage share BattleViewModel.");

        var miniHudPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        miniHudPanel.AddThemeStyleboxOverride("panel", ShowcaseTheme.HudStyle());
        _miniHud = new BattleMiniHud();
        miniHudPanel.AddChild(_miniHud);
        root.AddChild(miniHudPanel);

        _mainHud = new BattleMainHud();
        root.AddChild(ShowcaseUi.CreateCard(_mainHud));
    }
}
