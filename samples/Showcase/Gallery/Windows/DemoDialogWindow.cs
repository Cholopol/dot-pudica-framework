using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>
/// WindowType.Dialog — explicit OK/Cancel; result flows back through <see cref="Result"/>.
/// </summary>
public partial class DemoDialogWindow : GodotWindow
{
    /// <summary>true = OK, false = Cancel, null until closed.</summary>
    public bool? Result { get; private set; }

    public DemoDialogWindow()
    {
        WindowType = WindowType.Dialog;
        WindowName = "DemoDialog";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        var content = DemoOverlayChrome.Build(this, new DemoOverlayChrome.PanelContent
        {
            Accent = DemoOverlayChrome.DialogAccent,
            Title = "Dialog",
            Body = "Dialog requires an explicit choice — OK/Cancel writes Result back to the lab strip.",
            PanelSize = new Vector2(360, 170),
            BackdropAlpha = 0.45f
        });

        DemoOverlayChrome.AddButtonRow(
            content,
            ("OK", () => CloseWithResult(true)),
            ("Cancel", () => CloseWithResult(false)));
    }

    private void CloseWithResult(bool accepted)
    {
        Result = accepted;
        Dismiss();
    }
}
