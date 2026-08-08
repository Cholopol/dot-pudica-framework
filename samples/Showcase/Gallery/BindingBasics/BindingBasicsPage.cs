using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Binding.Converters;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.BindingBasics;

/// <summary>
/// Binding basics: four modes + built-in converters.
/// </summary>
[DotPudicaView(typeof(BindingBasicsViewModel))]
public partial class BindingBasicsPage : ShowcasePageWindow
{
    [Export, BindTo(nameof(BindingBasicsViewModel.Counter), Converter = typeof(IntToStringConverter))]
    private Label _counterLabel = null!;

    [Export, BindCommand(nameof(BindingBasicsViewModel.IncrementCommand))]
    private Button _incrementButton = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.UserName))]
    private LineEdit _userNameInput = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.UserName))]
    private Label _userNameMirrorLabel = null!;

    [Export, BindCommand(nameof(BindingBasicsViewModel.OverwriteUserNameFromVmCommand))]
    private Button _overwriteUserNameButton = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.RawInput), Mode = BindingMode.OneWayToSource)]
    private LineEdit _rawInputField = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.RawInput))]
    private Label _rawInputEchoLabel = null!;

    [Export, BindCommand(nameof(BindingBasicsViewModel.OverwriteRawInputFromVmCommand))]
    private Button _overwriteRawInputButton = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.InitialSeed), Mode = BindingMode.OneTime, Converter = typeof(IntToStringConverter))]
    private Label _initialSeedOnceLabel = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.InitialSeed), Converter = typeof(IntToStringConverter))]
    private Label _initialSeedLiveLabel = null!;

    [Export, BindCommand(nameof(BindingBasicsViewModel.RegenerateSeedCommand))]
    private Button _regenerateSeedButton = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.IsFeatureEnabled))]
    private CheckBox _featureCheckBox = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.IsFeatureEnabled), Mode = BindingMode.OneWay, Converter = typeof(BoolNegateConverter))]
    private CheckBox _featureNegatedCheckBox = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.ShowDetails))]
    private CheckBox _showDetailsCheckBox = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.ShowDetails), Target = "Visible", Converter = typeof(BoolToVisibilityConverter))]
    private PanelContainer _detailsPanel = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.SearchText))]
    private LineEdit _searchInput = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.SearchText), Mode = BindingMode.OneWay, Converter = typeof(StringToBoolConverter))]
    private CheckBox _hasSearchTextCheckBox = null!;

    [Export, BindCommand(nameof(BindingBasicsViewModel.BumpProgressCommand))]
    private Button _bumpProgressButton = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.ProgressRatio), Converter = typeof(FloatToStringConverter))]
    private Label _progressLabel = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.LastAction), Converter = typeof(ObjectToStringConverter))]
    private Label _lastActionLabel = null!;

    [Export, BindTo(nameof(BindingBasicsViewModel.IsSelectionCleared), Target = "Visible", Mode = BindingMode.OneWay)]
    private PanelContainer _selectionClearedHintPanel = null!;

    [Export, BindCommand(nameof(BindingBasicsViewModel.ClearSelectionCommand))]
    private Button _clearSelectionButton = null!;

    [Export, BindCommand(nameof(BindingBasicsViewModel.SelectOptionACommand))]
    private Button _selectOptionAButton = null!;

    [Export, BindCommand(nameof(BindingBasicsViewModel.SelectOptionBCommand))]
    private Button _selectOptionBButton = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this, scroll: true);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "Binding modes and built-in converters.");

        var metrics = ShowcaseUi.AddMetricsRow(root);
        ShowcaseUi.AddMetricChip(metrics, "Last Action", out _lastActionLabel);

        root.AddChild(new HSeparator());

        ShowcaseUi.AddSection(root, "OneWay");
        var counterRow = ShowcaseUi.AddRow(root);
        _counterLabel = new Label { Text = "0", CustomMinimumSize = new Vector2(48, 0), Modulate = ShowcaseTheme.Text };
        _incrementButton = ShowcaseUi.CreatePrimaryButton("Counter +1");
        counterRow.AddChild(_counterLabel);
        counterRow.AddChild(_incrementButton);

        ShowcaseUi.AddSection(root, "TwoWay");
        var userNameRow = ShowcaseUi.AddRow(root);
        _userNameInput = new LineEdit { PlaceholderText = "User name…", CustomMinimumSize = new Vector2(180, 0) };
        _userNameMirrorLabel = new Label { Text = "", CustomMinimumSize = new Vector2(100, 0) };
        _overwriteUserNameButton = ShowcaseUi.CreateActionButton("Write from VM");
        userNameRow.AddChild(_userNameInput);
        userNameRow.AddChild(_userNameMirrorLabel);
        userNameRow.AddChild(_overwriteUserNameButton);

        ShowcaseUi.AddSection(root, "OneWayToSource");
        var rawInputRow = ShowcaseUi.AddRow(root);
        _rawInputField = new LineEdit
        {
            PlaceholderText = "Type, then write from VM…",
            CustomMinimumSize = new Vector2(180, 0)
        };
        _rawInputEchoLabel = new Label { Text = "(empty)", CustomMinimumSize = new Vector2(140, 0) };
        _overwriteRawInputButton = ShowcaseUi.CreateActionButton("Write RawInput");
        rawInputRow.AddChild(_rawInputField);
        rawInputRow.AddChild(_rawInputEchoLabel);
        rawInputRow.AddChild(_overwriteRawInputButton);

        ShowcaseUi.AddSection(root, "OneTime");
        var seedRow = ShowcaseUi.AddRow(root);
        _initialSeedOnceLabel = new Label { Text = "0", CustomMinimumSize = new Vector2(64, 0) };
        _initialSeedLiveLabel = new Label { Text = "0", CustomMinimumSize = new Vector2(64, 0) };
        _regenerateSeedButton = ShowcaseUi.CreatePrimaryButton("Regenerate Seed");
        seedRow.AddChild(new Label { Text = "OneTime", Modulate = ShowcaseTheme.Muted });
        seedRow.AddChild(_initialSeedOnceLabel);
        seedRow.AddChild(new Label { Text = "Live", Modulate = ShowcaseTheme.Muted });
        seedRow.AddChild(_initialSeedLiveLabel);
        seedRow.AddChild(_regenerateSeedButton);

        ShowcaseUi.AddSection(root, "BoolNegate");
        var featureRow = ShowcaseUi.AddRow(root);
        _featureCheckBox = new CheckBox { Text = "Feature" };
        _featureNegatedCheckBox = new CheckBox { Text = "Negated (read-only)", Disabled = true };
        featureRow.AddChild(_featureCheckBox);
        featureRow.AddChild(_featureNegatedCheckBox);

        ShowcaseUi.AddSection(root, "BoolToVisibility");
        var detailsRow = ShowcaseUi.AddRow(root);
        _showDetailsCheckBox = new CheckBox { Text = "Show details", ButtonPressed = true };
        detailsRow.AddChild(_showDetailsCheckBox);
        var detailsBody = ShowcaseUi.CreateCardBody(out _detailsPanel);
        detailsBody.AddChild(new Label { Text = "Details panel content." });
        root.AddChild(_detailsPanel);

        ShowcaseUi.AddSection(root, "StringToBool");
        var searchRow = ShowcaseUi.AddRow(root);
        _searchInput = new LineEdit { PlaceholderText = "Type to toggle indicator…", CustomMinimumSize = new Vector2(180, 0) };
        _hasSearchTextCheckBox = new CheckBox { Text = "Has text (read-only)", Disabled = true };
        searchRow.AddChild(_searchInput);
        searchRow.AddChild(_hasSearchTextCheckBox);

        ShowcaseUi.AddSection(root, "FloatToString");
        var progressRow = ShowcaseUi.AddRow(root);
        _bumpProgressButton = ShowcaseUi.CreatePrimaryButton("Bump Progress");
        _progressLabel = new Label { Text = "0.00", CustomMinimumSize = new Vector2(64, 0) };
        progressRow.AddChild(_bumpProgressButton);
        progressRow.AddChild(_progressLabel);

        ShowcaseUi.AddSection(root, "Equality");
        var selectionRow = ShowcaseUi.AddActionRow(root);
        _selectOptionAButton = ShowcaseUi.CreateActionButton("Select A");
        _selectOptionBButton = ShowcaseUi.CreateActionButton("Select B");
        _clearSelectionButton = ShowcaseUi.CreateActionButton("Clear");
        selectionRow.AddChild(_selectOptionAButton);
        selectionRow.AddChild(_selectOptionBButton);
        selectionRow.AddChild(_clearSelectionButton);
        var clearedBody = ShowcaseUi.CreateCardBody(out _selectionClearedHintPanel);
        clearedBody.AddChild(new Label { Text = "No option selected.", Modulate = ShowcaseTheme.Muted });
        root.AddChild(_selectionClearedHintPanel);
    }
}
