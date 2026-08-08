using Godot;
using Samples.Showcase.Shared.Probes;

namespace Samples.Showcase.Gallery.ThreadingLab;

/// <summary>
/// Probe card — title, expectation, observed output, status badge, and Run button.
/// </summary>
public partial class ProbeCard : PanelContainer
{
    private readonly Label _titleLabel;
    private readonly Label _badgeLabel;
    private readonly Label _expectationLabel;
    private readonly Label _observedLabel;
    private readonly Button _runButton;

    /// <summary>Raised when Run is pressed; page supplies the probe logic.</summary>
    public event Action? RunRequested;

    public ProbeCard(string id, string name, string expectation)
    {
        CustomMinimumSize = new Vector2(0, 0);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", ShowcaseTheme.PanelStyle(10));

        var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        vbox.AddThemeConstantOverride("separation", 8);
        AddChild(vbox);

        var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(header);

        _titleLabel = new Label
        {
            Text = $"[{id}] {name}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Modulate = ShowcaseTheme.Text
        };
        header.AddChild(_titleLabel);

        _badgeLabel = new Label { Text = "Idle", Modulate = ShowcaseTheme.Muted };
        header.AddChild(_badgeLabel);

        _runButton = new Button
        {
            Text = "Run",
            CustomMinimumSize = new Vector2(72, ShowcaseTheme.ActionHeight)
        };
        ShowcaseUi.ApplyPrimaryButton(_runButton);
        _runButton.Pressed += () => RunRequested?.Invoke();
        header.AddChild(_runButton);

        _expectationLabel = new Label
        {
            Text = $"Expect: {expectation}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        vbox.AddChild(_expectationLabel);

        _observedLabel = new Label
        {
            Text = "Observed: —",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Text
        };
        vbox.AddChild(_observedLabel);
    }

    /// <summary>Disable Run and show in-progress state.</summary>
    public void SetRunning()
    {
        _runButton.Disabled = true;
        _badgeLabel.Text = "Running…";
        _badgeLabel.Modulate = ShowcaseTheme.Warning;
        _observedLabel.Text = "Observed: Running…";
    }

    /// <summary>Apply probe verdict and re-enable Run.</summary>
    public void ApplyResult(ProbeResult result)
    {
        _runButton.Disabled = false;
        _expectationLabel.Text = $"Expect: {result.Expectation}";
        _observedLabel.Text = $"Observed: {result.Observed}";
        (_badgeLabel.Text, _badgeLabel.Modulate) = result.Verdict switch
        {
            ProbeVerdict.Pass => ("PASS", ShowcaseTheme.Success),
            ProbeVerdict.Fail => ("FAIL", ShowcaseTheme.Danger),
            ProbeVerdict.Evidence => ("EVIDENCE", new Color(0.4f, 0.7f, 0.95f)),
            _ => ("?", ShowcaseTheme.Muted)
        };
    }

    /// <summary>Unhandled exception — skip probe verdict, mark ERROR.</summary>
    public void ApplyError(string message)
    {
        _runButton.Disabled = false;
        _observedLabel.Text = $"Observed: error — {message}";
        _badgeLabel.Text = "ERROR";
        _badgeLabel.Modulate = ShowcaseTheme.Danger;
    }
}
