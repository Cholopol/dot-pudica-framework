using Godot;

namespace Samples.Showcase;

/// <summary>
/// Right-side metrics strip for Windows lab — stays above ContentHost/OverlayHost.
/// </summary>
public partial class ShowcaseLabHud : PanelContainer
{
    private readonly Label _pageStatusValue;
    private readonly Label _guideValue;
    private readonly Label _stackValue;
    private readonly Label _resultValue;

    public Button FindDialogButton { get; }
    public Button ClearOverlaysButton { get; }

    public ShowcaseLabHud()
    {
        Name = "LabHud";
        Visible = false;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(0, 0);
        AddThemeStyleboxOverride("panel", ShowcaseTheme.HudStyle());

        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        var header = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddThemeConstantOverride("separation", 12);
        root.AddChild(header);

        header.AddChild(new Label
        {
            Text = "WINDOW LAB",
            Modulate = ShowcaseTheme.BannerAccent,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        });

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(spacer);

        FindDialogButton = ShowcaseUi.CreateActionButton("Find Dialog");
        header.AddChild(FindDialogButton);
        ClearOverlaysButton = ShowcaseUi.CreatePrimaryButton("Clear Overlays");
        header.AddChild(ClearOverlaysButton);

        // Status + Dialog share the first metrics row with balanced widths.
        var topMetrics = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topMetrics.AddThemeConstantOverride("separation", 10);
        root.AddChild(topMetrics);

        _pageStatusValue = AddMetric(topMetrics, "Status", stretchRatio: 2.5f, minWidth: 280);
        _resultValue = AddMetric(topMetrics, "Dialog", stretchRatio: 1f, minWidth: 140);

        // Stack needs horizontal room for multi-line dumps — full width below.
        _stackValue = AddMetric(root, "Stack", stretchRatio: 1f, minWidth: 0, expandFill: true);

        _guideValue = new Label
        {
            Text = "—",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        root.AddChild(_guideValue);
    }

    private static Label AddMetric(
        Container parent,
        string key,
        float stretchRatio,
        float minWidth,
        bool expandFill = true)
    {
        var chip = new PanelContainer
        {
            SizeFlagsHorizontal = expandFill ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin,
            SizeFlagsStretchRatio = stretchRatio,
            CustomMinimumSize = new Vector2(minWidth, 0)
        };
        chip.AddThemeStyleboxOverride("panel", ShowcaseTheme.MetricStyle());
        parent.AddChild(chip);

        var inner = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 2);
        chip.AddChild(inner);

        inner.AddChild(new Label { Text = key, Modulate = ShowcaseTheme.Muted });
        var value = new Label
        {
            Text = "—",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        inner.AddChild(value);
        return value;
    }

    public void ShowForWindowsLab()
    {
        Visible = true;
        Reset();
    }

    public void HideAndReset()
    {
        Visible = false;
        Reset();
    }

    public void Reset()
    {
        _pageStatusValue.Text = "—";
        _guideValue.Text = "—";
        _resultValue.Text = "—";
        _stackValue.Text = "—";
    }

    public void SetPageStatus(string text) => _pageStatusValue.Text = text;

    public void SetGuide(string text) => _guideValue.Text = text;

    public void SetResult(string text) => _resultValue.Text = text;

    public void SetStack(string text) => _stackValue.Text = text;
}
