using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>WindowType.Progress — auto-advances and dismisses at 100%; cancelable.</summary>
public partial class DemoProgressWindow : GodotWindow
{
    private ProgressBar _bar = null!;
    private double _value;

    public DemoProgressWindow()
    {
        WindowType = WindowType.Progress;
        WindowName = "DemoProgress";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        DemoOverlayChrome.Build(this, new DemoOverlayChrome.PanelContent
        {
            Accent = DemoOverlayChrome.ProgressAccent,
            Title = "Progress",
            Body = "Progress auto-closes when full — same stack semantics as Popup/Dialog.",
            PanelSize = new Vector2(340, 160),
            BackdropAlpha = 0.5f,
            BuildExtra = vbox =>
            {
                _bar = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 0 };
                vbox.AddChild(_bar);
                DemoOverlayChrome.AddCloseButton(vbox, "Cancel", () => Dismiss());
            }
        });
    }

    public override void _Process(double delta)
    {
        if (Dismissed || IsDismissing)
            return;

        _value += delta * 60.0;
        _bar.Value = Math.Min(_value, 100);
        if (_value >= 100)
            Dismiss();
    }
}
