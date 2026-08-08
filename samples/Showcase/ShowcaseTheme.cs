using Godot;

namespace Samples.Showcase;

/// <summary>Showcase visual tokens — cool slate + teal accent, not purple/glow defaults.</summary>
public static class ShowcaseTheme
{
    public static readonly Color BannerBg = new(0.05f, 0.06f, 0.08f);
    public static readonly Color BannerAccent = new(0.28f, 0.72f, 0.78f);
    public static readonly Color NavBg = new(0.08f, 0.09f, 0.11f);
    public static readonly Color ContentBg = new(0.13f, 0.14f, 0.16f);
    public static readonly Color PageBg = new(0.11f, 0.12f, 0.14f);
    public static readonly Color PanelBg = new(0.15f, 0.16f, 0.19f);
    public static readonly Color PanelBorder = new(0.22f, 0.24f, 0.28f);
    public static readonly Color MetricBg = new(0.1f, 0.12f, 0.14f);
    public static readonly Color Section = new(0.55f, 0.78f, 0.74f);
    public static readonly Color Muted = new(0.58f, 0.62f, 0.66f);
    public static readonly Color Text = new(0.92f, 0.93f, 0.94f);
    public static readonly Color Danger = new(0.9f, 0.42f, 0.42f);
    public static readonly Color Success = new(0.38f, 0.82f, 0.52f);
    public static readonly Color Warning = new(0.9f, 0.75f, 0.35f);

    public const int BannerHeight = 44;
    public const int PageMargin = 20;
    public const int Separation = 12;
    public const int ActionHeight = 36;
    public const int NavWidth = 220;

    public static StyleBoxFlat BannerStyle() => Flat(
        BannerBg,
        contentLeft: 20, contentTop: 0, contentRight: 20, contentBottom: 0,
        borderBottom: 2, borderColor: BannerAccent);

    public static StyleBoxFlat NavStyle() => Flat(NavBg);

    public static StyleBoxFlat PanelStyle(int pad = 12) => Flat(
        PanelBg,
        contentLeft: pad, contentTop: pad, contentRight: pad, contentBottom: pad,
        border: 1, borderColor: PanelBorder, radius: 4);

    public static StyleBoxFlat MetricStyle() => Flat(
        MetricBg,
        contentLeft: 12, contentTop: 8, contentRight: 12, contentBottom: 8,
        border: 1, borderColor: PanelBorder, radius: 4);

    public static StyleBoxFlat HudStyle() => Flat(
        BannerBg,
        contentLeft: 16, contentTop: 10, contentRight: 16, contentBottom: 10,
        borderBottom: 1, borderColor: PanelBorder);

    public static StyleBoxFlat PrimaryButtonNormal() => Flat(
        new Color(0.18f, 0.42f, 0.46f),
        contentLeft: 14, contentTop: 8, contentRight: 14, contentBottom: 8,
        radius: 4);

    public static StyleBoxFlat PrimaryButtonHover() => Flat(
        new Color(0.22f, 0.52f, 0.56f),
        contentLeft: 14, contentTop: 8, contentRight: 14, contentBottom: 8,
        radius: 4);

    public static StyleBoxFlat PrimaryButtonPressed() => Flat(
        new Color(0.14f, 0.34f, 0.38f),
        contentLeft: 14, contentTop: 8, contentRight: 14, contentBottom: 8,
        radius: 4);

    public static StyleBoxFlat ActionButtonNormal() => Flat(
        new Color(0.2f, 0.22f, 0.26f),
        contentLeft: 14, contentTop: 8, contentRight: 14, contentBottom: 8,
        border: 1, borderColor: PanelBorder, radius: 4);

    public static StyleBoxFlat ActionButtonHover() => Flat(
        new Color(0.26f, 0.29f, 0.34f),
        contentLeft: 14, contentTop: 8, contentRight: 14, contentBottom: 8,
        border: 1, borderColor: BannerAccent, radius: 4);

    public static StyleBoxFlat ActionButtonPressed() => Flat(
        new Color(0.16f, 0.18f, 0.21f),
        contentLeft: 14, contentTop: 8, contentRight: 14, contentBottom: 8,
        border: 1, borderColor: PanelBorder, radius: 4);

    private static StyleBoxFlat Flat(
        Color bg,
        int contentLeft = 0, int contentTop = 0, int contentRight = 0, int contentBottom = 0,
        int border = 0, Color? borderColor = null,
        int borderBottom = 0, int radius = 0)
    {
        var style = new StyleBoxFlat
        {
            BgColor = bg,
            ContentMarginLeft = contentLeft,
            ContentMarginTop = contentTop,
            ContentMarginRight = contentRight,
            ContentMarginBottom = contentBottom,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius
        };

        if (border > 0 && borderColor is { } c)
        {
            style.BorderWidthLeft = border;
            style.BorderWidthTop = border;
            style.BorderWidthRight = border;
            style.BorderWidthBottom = border;
            style.BorderColor = c;
        }

        if (borderBottom > 0 && borderColor is { } bc)
        {
            style.BorderWidthBottom = borderBottom;
            style.BorderColor = bc;
        }

        return style;
    }
}
