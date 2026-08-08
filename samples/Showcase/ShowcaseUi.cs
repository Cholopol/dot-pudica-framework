using Godot;

namespace Samples.Showcase;

/// <summary>Shared Gallery layout builders: page body, metrics, actions, sections.</summary>
public static class ShowcaseUi
{
    public sealed class PageBody
    {
        public required MarginContainer Margin { get; init; }
        public required VBoxContainer Root { get; init; }
        public ScrollContainer? Scroll { get; init; }
    }

    public static PanelContainer CreateTopBanner(out Label titleLabel)
    {
        var banner = new PanelContainer
        {
            Name = "TopBanner",
            CustomMinimumSize = new Vector2(0, ShowcaseTheme.BannerHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        banner.AddThemeStyleboxOverride("panel", ShowcaseTheme.BannerStyle());

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.End
        };
        banner.AddChild(row);

        titleLabel = new Label
        {
            Name = "BannerTitle",
            Text = "Showcase.Pudica",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Modulate = ShowcaseTheme.Text
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        row.AddChild(titleLabel);

        var brand = new Label
        {
            Name = "BrandLabel",
            Text = "DotPudica Framework",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            Modulate = ShowcaseTheme.BannerAccent
        };
        brand.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(brand);

        return banner;
    }

    public static PageBody AttachPageBody(Control page, bool scroll = false)
    {
        var margin = new MarginContainer { Name = "PageMargin" };
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", ShowcaseTheme.PageMargin);
        margin.AddThemeConstantOverride("margin_top", ShowcaseTheme.PageMargin);
        margin.AddThemeConstantOverride("margin_right", ShowcaseTheme.PageMargin);
        margin.AddThemeConstantOverride("margin_bottom", ShowcaseTheme.PageMargin);
        page.AddChild(margin);

        ScrollContainer? scrollNode = null;
        Control host = margin;
        if (scroll)
        {
            scrollNode = new ScrollContainer
            {
                Name = "PageScroll",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };
            margin.AddChild(scrollNode);
            host = scrollNode;
        }

        var root = new VBoxContainer
        {
            Name = "PageRoot",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", ShowcaseTheme.Separation);
        host.AddChild(root);

        return new PageBody { Margin = margin, Root = root, Scroll = scrollNode };
    }

    public static Label AddSubtitle(VBoxContainer root, string text)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        root.AddChild(label);
        return label;
    }

    public static HBoxContainer AddMetricsRow(VBoxContainer root)
    {
        var row = new HBoxContainer
        {
            Name = "MetricsRow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 10);
        root.AddChild(row);
        return row;
    }

    public static Label AddMetricChip(HBoxContainer row, string key, out Label valueLabel, bool expand = false)
    {
        var chip = new PanelContainer
        {
            SizeFlagsHorizontal = expand
                ? Control.SizeFlags.ExpandFill
                : Control.SizeFlags.ShrinkBegin
        };
        chip.AddThemeStyleboxOverride("panel", ShowcaseTheme.MetricStyle());
        row.AddChild(chip);

        var inner = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        inner.AddThemeConstantOverride("separation", 8);
        chip.AddChild(inner);

        inner.AddChild(new Label
        {
            Text = key,
            Modulate = ShowcaseTheme.Muted
        });

        valueLabel = new Label
        {
            Text = "—",
            Modulate = ShowcaseTheme.Text,
            AutowrapMode = TextServer.AutowrapMode.Off,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        inner.AddChild(valueLabel);
        return valueLabel;
    }

    public static HBoxContainer AddActionRow(VBoxContainer root)
    {
        var row = new HBoxContainer
        {
            Name = "ActionRow",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 10);
        root.AddChild(row);
        return row;
    }

    public static Button CreatePrimaryButton(string text)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, ShowcaseTheme.ActionHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
        };
        ApplyPrimaryButton(btn);
        return btn;
    }

    public static Button CreateActionButton(string text, bool expand = false)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, ShowcaseTheme.ActionHeight),
            SizeFlagsHorizontal = expand
                ? Control.SizeFlags.ExpandFill
                : Control.SizeFlags.ShrinkBegin
        };
        ApplyActionButton(btn);
        return btn;
    }

    public static void ApplyPrimaryButton(Button btn)
    {
        btn.AddThemeStyleboxOverride("normal", ShowcaseTheme.PrimaryButtonNormal());
        btn.AddThemeStyleboxOverride("hover", ShowcaseTheme.PrimaryButtonHover());
        btn.AddThemeStyleboxOverride("pressed", ShowcaseTheme.PrimaryButtonPressed());
        btn.AddThemeStyleboxOverride("focus", ShowcaseTheme.PrimaryButtonHover());
        btn.AddThemeColorOverride("font_color", ShowcaseTheme.Text);
        btn.AddThemeColorOverride("font_hover_color", Colors.White);
        btn.AddThemeColorOverride("font_pressed_color", ShowcaseTheme.Text);
    }

    public static void ApplyActionButton(Button btn)
    {
        btn.AddThemeStyleboxOverride("normal", ShowcaseTheme.ActionButtonNormal());
        btn.AddThemeStyleboxOverride("hover", ShowcaseTheme.ActionButtonHover());
        btn.AddThemeStyleboxOverride("pressed", ShowcaseTheme.ActionButtonPressed());
        btn.AddThemeStyleboxOverride("focus", ShowcaseTheme.ActionButtonHover());
        btn.AddThemeColorOverride("font_color", ShowcaseTheme.Text);
        btn.AddThemeColorOverride("font_hover_color", Colors.White);
        btn.AddThemeColorOverride("font_pressed_color", ShowcaseTheme.Text);
    }

    public static Label AddSection(VBoxContainer root, string title)
    {
        var label = new Label
        {
            Text = title,
            Modulate = ShowcaseTheme.Section
        };
        root.AddChild(label);
        return label;
    }

    public static HBoxContainer AddRow(VBoxContainer root)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 10);
        root.AddChild(row);
        return row;
    }

    public static PanelContainer CreateCard(Control content, int pad = 12)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", ShowcaseTheme.PanelStyle(pad));
        panel.AddChild(content);
        return panel;
    }

    public static VBoxContainer CreateCardBody(out PanelContainer panel, int pad = 12)
    {
        panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", ShowcaseTheme.PanelStyle(pad));
        var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 8);
        panel.AddChild(body);
        return body;
    }
}
