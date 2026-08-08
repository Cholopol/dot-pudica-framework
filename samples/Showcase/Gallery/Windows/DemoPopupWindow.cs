using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>WindowType.Popup — stacks over content without hiding the page below.</summary>
public partial class DemoPopupWindow : GodotWindow
{
    public DemoPopupWindow(string? subtitle = null)
    {
        WindowType = WindowType.Popup;
        WindowName = "DemoPopup";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        _subtitle = subtitle;
    }

    private readonly string? _subtitle;

    public override void _Ready()
    {
        var body = string.IsNullOrEmpty(_subtitle)
            ? "Popup stacks over content; the page below stays alive after dismiss."
            : _subtitle!;

        var content = DemoOverlayChrome.Build(this, new DemoOverlayChrome.PanelContent
        {
            Accent = DemoOverlayChrome.PopupAccent,
            Title = "Popup",
            Body = body,
            PanelSize = new Vector2(340, 150),
            BackdropAlpha = 0.35f
        });

        DemoOverlayChrome.AddCloseButton(content, "Close", () => Dismiss());
    }
}
