using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Binding.Converters;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Validation;

/// <summary>
/// Validation gallery: DataAnnotations + ValidatableViewModelBase, HasErrors / GetErrors.
/// </summary>
[DotPudicaView(typeof(ValidationViewModel))]
public partial class ValidationPage : ShowcasePageWindow
{
    [Export, BindTo(nameof(ValidationViewModel.Username))]
    private LineEdit _usernameInput = null!;

    [Export, BindTo(nameof(ValidationViewModel.Email))]
    private LineEdit _emailInput = null!;

    [Export, BindTo(nameof(ValidationViewModel.Age))]
    private SpinBox _ageInput = null!;

    [Export, BindTo(nameof(ValidationViewModel.HasErrors), Target = "Visible", Converter = typeof(BoolToVisibilityConverter))]
    private PanelContainer _hasErrorsPanel = null!;

    [Export, BindTo(nameof(ValidationViewModel.ErrorSummary))]
    private Label _errorSummaryLabel = null!;

    [Export, BindCommand(nameof(ValidationViewModel.ValidateAllNowCommand))]
    private Button _validateAllButton = null!;

    [Export, BindCommand(nameof(ValidationViewModel.SubmitCommand))]
    private Button _submitButton = null!;

    [Export, BindTo(nameof(ValidationViewModel.SummaryText))]
    private Label _summaryLabel = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady() => EnsureControls();

    private void EnsureControls()
    {
        var body = ShowcaseUi.AttachPageBody(this, scroll: true);
        var root = body.Root;

        ShowcaseUi.AddSubtitle(root, "DataAnnotations validation with CanExecute-gated submit.");

        var form = ShowcaseUi.CreateCardBody(out var formPanel);
        root.AddChild(formPanel);

        form.AddChild(new Label { Text = "Username", Modulate = ShowcaseTheme.Muted });
        _usernameInput = new LineEdit { PlaceholderText = "At least 3 characters…" };
        form.AddChild(_usernameInput);

        form.AddChild(new Label { Text = "Email", Modulate = ShowcaseTheme.Muted });
        _emailInput = new LineEdit { PlaceholderText = "name@example.com" };
        form.AddChild(_emailInput);

        form.AddChild(new Label { Text = "Age", Modulate = ShowcaseTheme.Muted });
        _ageInput = new SpinBox { MinValue = 0, MaxValue = 200, Step = 1 };
        form.AddChild(_ageInput);

        var actionRow = ShowcaseUi.AddActionRow(form);
        _submitButton = ShowcaseUi.CreatePrimaryButton("Submit");
        _validateAllButton = ShowcaseUi.CreateActionButton("Validate");
        actionRow.AddChild(_submitButton);
        actionRow.AddChild(_validateAllButton);

        var errorBody = ShowcaseUi.CreateCardBody(out _hasErrorsPanel);
        errorBody.AddChild(new Label { Text = "Validation errors", Modulate = ShowcaseTheme.Danger });
        _errorSummaryLabel = new Label { Text = "", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        errorBody.AddChild(_errorSummaryLabel);
        root.AddChild(_hasErrorsPanel);

        _summaryLabel = new Label { Text = "", AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = ShowcaseTheme.Muted };
        root.AddChild(_summaryLabel);
    }
}
