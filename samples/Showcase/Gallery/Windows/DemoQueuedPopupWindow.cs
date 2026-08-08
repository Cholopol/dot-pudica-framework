using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>
/// WindowType.QueuedPopup — FIFO queue; next shows after the current one dismisses.
/// </summary>
public partial class DemoQueuedPopupWindow : GodotWindow
{
    private readonly int _index;

    public DemoQueuedPopupWindow(int index)
    {
        _index = index;
        WindowType = WindowType.QueuedPopup;
        WindowName = $"QueuedPopup#{index}";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public int Index => _index;

    public override void _Ready()
    {
        var content = DemoOverlayChrome.Build(this, new DemoOverlayChrome.PanelContent
        {
            Accent = DemoOverlayChrome.QueuedAccent,
            Title = $"Queued Popup #{_index}",
            Body = "QueuedPopup — only one visible; close to dequeue the next.",
            PanelSize = new Vector2(320, 150),
            BackdropAlpha = 0.4f
        });

        DemoOverlayChrome.AddCloseButton(content, "Close (next in queue)", () => Dismiss());
    }
}
