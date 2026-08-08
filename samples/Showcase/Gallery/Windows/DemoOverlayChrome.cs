using Godot;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>
/// Shared backdrop + centered panel for overlay windows; each Demo*Window only passes accent color and content, avoiding four copies of boilerplate.
/// </summary>
internal static class DemoOverlayChrome
{
    public static readonly Color PopupAccent = new(0.35f, 0.65f, 0.95f);
    public static readonly Color DialogAccent = new(0.95f, 0.7f, 0.25f);
    public static readonly Color ProgressAccent = new(0.45f, 0.85f, 0.55f);
    public static readonly Color QueuedAccent = new(0.8f, 0.5f, 0.95f);

    public readonly struct PanelContent
    {
        public required Color Accent { get; init; }
        public required string Title { get; init; }
        public required string Body { get; init; }
        public Vector2 PanelSize { get; init; }
        public float BackdropAlpha { get; init; }
        public Action<VBoxContainer>? BuildExtra { get; init; }
    }

    /// <summary>
    /// Attaches a full-screen backdrop and centered panel to the host; returns the content VBox (already containing title/body).
    /// </summary>
    public static VBoxContainer Build(Control host, in PanelContent content)
    {
        var size = content.PanelSize == default ? new Vector2(340, 160) : content.PanelSize;
        var alpha = content.BackdropAlpha > 0 ? content.BackdropAlpha : 0.4f;

        var backdrop = new ColorRect
        {
            Color = new Color(0, 0, 0, alpha),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        host.AddChild(backdrop);

        var panel = new PanelContainer { CustomMinimumSize = size };
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        host.AddChild(panel);

        var accentBar = new ColorRect
        {
            Color = content.Accent,
            CustomMinimumSize = new Vector2(0, 4),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 8);
        margin.AddChild(outer);
        outer.AddChild(accentBar);

        var title = new Label
        {
            Text = content.Title,
            Modulate = content.Accent
        };
        outer.AddChild(title);

        outer.AddChild(new Label
        {
            Text = content.Body,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        content.BuildExtra?.Invoke(outer);
        return outer;
    }

    public static Button AddCloseButton(VBoxContainer content, string text, Action onPressed)
    {
        var button = new Button { Text = text };
        button.Pressed += onPressed;
        content.AddChild(button);
        return button;
    }

    public static HBoxContainer AddButtonRow(VBoxContainer content, params (string Text, Action OnPressed)[] buttons)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        content.AddChild(row);
        foreach (var (text, onPressed) in buttons)
        {
            var button = new Button { Text = text };
            button.Pressed += onPressed;
            row.AddChild(button);
        }

        return row;
    }
}
