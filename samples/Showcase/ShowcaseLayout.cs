using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase;

/// <summary>
/// Showcase layout:
/// - Full pages attach to <see cref="ContentHost"/>
/// - Overlay windows attach to <see cref="OverlayHost"/> (content area only)
/// - Live metrics write to <see cref="LabHud"/> so Hide/modals cannot cover them
/// </summary>
public static class ShowcaseLayout
{
    public static Control? ContentHost { get; set; }
    public static Control? OverlayHost { get; set; }
    public static ShowcaseLabHud? LabHud { get; set; }

    /// <summary>Shell wires this to update the fixed top banner title.</summary>
    public static Action<string>? BannerTitleSetter { get; set; }

    public static void SetBannerInstance(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
            return;
        BannerTitleSetter?.Invoke($"{instanceName}.Pudica");
    }

    /// <summary>Called before <see cref="IWindowManager.Show"/>: attach to ContentHost first, then let Manager handle only stack logic.</summary>
    public static void PrepareFullPage(GodotWindow page)
    {
        if (ContentHost is null || !GodotObject.IsInstanceValid(ContentHost))
            return;
        if (page.WindowType != WindowType.Full)
            return;

        if (page.GetParent() == null)
            ContentHost.AddChild(page);
        else if (page.GetParent() != ContentHost)
            page.Reparent(ContentHost);

        ApplyFullRect(page);
    }

    /// <summary>
    /// Attach overlay to OverlayHost before Show, so it only covers the right content area;
    /// Manager will not re-parent it to fill the full window once it already has a parent.
    /// </summary>
    public static void PrepareOverlay(GodotWindow overlay)
    {
        if (OverlayHost is null || !GodotObject.IsInstanceValid(OverlayHost))
            return;
        if (overlay.WindowType == WindowType.Full)
            return;

        if (overlay.GetParent() == null)
            OverlayHost.AddChild(overlay);
        else if (overlay.GetParent() != OverlayHost)
            overlay.Reparent(OverlayHost);

        ApplyFullRect(overlay);
    }

    public static void AttachFullPage(GodotWindow page) => PrepareFullPage(page);

    private static void ApplyFullRect(GodotWindow page)
    {
        page.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        page.OffsetLeft = 0;
        page.OffsetTop = 0;
        page.OffsetRight = 0;
        page.OffsetBottom = 0;
        page.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        page.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        page.MouseFilter = Control.MouseFilterEnum.Stop;
    }
}
