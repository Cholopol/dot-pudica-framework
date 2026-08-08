using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Binding.Converters;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Commands;

/// <summary>
/// Commands gallery: RelayCommand, CanExecute, AsyncRelayCommand, parameterized commands.
/// </summary>
[DotPudicaView(typeof(CommandsViewModel))]
public partial class CommandsPage : ShowcasePageWindow
{
    [Export, BindTo(nameof(CommandsViewModel.ClickCount), Converter = typeof(IntToStringConverter))]
    private Label _clickCountLabel = null!;

    [Export, BindCommand(nameof(CommandsViewModel.IncrementCommand))]
    private Button _incrementButton = null!;

    [Export, BindTo(nameof(CommandsViewModel.IsUnlocked))]
    private CheckBox _unlockCheckBox = null!;

    [Export, BindCommand(nameof(CommandsViewModel.RunLockedCommand))]
    private Button _runLockedButton = null!;

    [Export, BindTo(nameof(CommandsViewModel.IsBusy), Target = "Visible", Converter = typeof(BoolToVisibilityConverter))]
    private PanelContainer _busyIndicatorPanel = null!;

    [Export, BindTo(nameof(CommandsViewModel.ResultText))]
    private Label _resultLabel = null!;

    [Export, BindCommand(nameof(CommandsViewModel.LoadDataCommand))]
    private Button _loadDataButton = null!;

    [Export, BindTo(nameof(CommandsViewModel.CurrentLevel), Converter = typeof(IntToStringConverter))]
    private Label _currentLevelLabel = null!;

    [Export, BindCommand(nameof(CommandsViewModel.SetLevelCommand), Parameter = nameof(CommandsViewModel.LevelOptionA))]
    private Button _levelAButton = null!;

    [Export, BindCommand(nameof(CommandsViewModel.SetLevelCommand), Parameter = nameof(CommandsViewModel.LevelOptionB))]
    private Button _levelBButton = null!;

    [Export, BindCommand(nameof(CommandsViewModel.SetLevelCommand), Parameter = nameof(CommandsViewModel.LevelOptionC))]
    private Button _levelCButton = null!;

    [Export, BindTo(nameof(CommandsViewModel.LastCommandLog))]
    private Label _lastCommandLogLabel = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this, scroll: true);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "RelayCommand, CanExecute, async, and parameterized commands.");

        var metrics = ShowcaseUi.AddMetricsRow(root);
        ShowcaseUi.AddMetricChip(metrics, "Clicks", out _clickCountLabel);
        ShowcaseUi.AddMetricChip(metrics, "Level", out _currentLevelLabel);
        ShowcaseUi.AddMetricChip(metrics, "Log", out _lastCommandLogLabel);

        root.AddChild(new HSeparator());

        ShowcaseUi.AddSection(root, "RelayCommand");
        var incrementRow = ShowcaseUi.AddActionRow(root);
        _incrementButton = ShowcaseUi.CreatePrimaryButton("Click +1");
        incrementRow.AddChild(_incrementButton);

        ShowcaseUi.AddSection(root, "CanExecute");
        var lockRow = ShowcaseUi.AddRow(root);
        _unlockCheckBox = new CheckBox { Text = "Unlock" };
        _runLockedButton = ShowcaseUi.CreateActionButton("Run gated command");
        lockRow.AddChild(_unlockCheckBox);
        lockRow.AddChild(_runLockedButton);

        ShowcaseUi.AddSection(root, "AsyncRelayCommand");
        var loadRow = ShowcaseUi.AddRow(root);
        _loadDataButton = ShowcaseUi.CreatePrimaryButton("Load async");
        loadRow.AddChild(_loadDataButton);
        var busyBody = ShowcaseUi.CreateCardBody(out _busyIndicatorPanel);
        busyBody.AddChild(new Label { Text = "Loading…", Modulate = ShowcaseTheme.Warning });
        loadRow.AddChild(_busyIndicatorPanel);
        _resultLabel = new Label { Text = "", AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = ShowcaseTheme.Muted };
        root.AddChild(_resultLabel);

        ShowcaseUi.AddSection(root, "Parameterized");
        var levelRow = ShowcaseUi.AddActionRow(root);
        _levelAButton = ShowcaseUi.CreateActionButton("Level 1");
        _levelBButton = ShowcaseUi.CreateActionButton("Level 2");
        _levelCButton = ShowcaseUi.CreateActionButton("Level 3");
        levelRow.AddChild(_levelAButton);
        levelRow.AddChild(_levelBButton);
        levelRow.AddChild(_levelCButton);
    }
}
