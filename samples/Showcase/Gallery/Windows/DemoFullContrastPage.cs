using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>
/// Full-page contrast — Show hides the Windows page; Dismiss restores it.
/// </summary>
public partial class DemoFullContrastPage : ShowcasePageWindow
{
    public DemoFullContrastPage()
    {
        WindowName = "FullContrast";
    }

    public override void _Ready()
    {
        var body = ShowcaseUi.AttachPageBody(this);
        var root = body.Root;

        ShowcaseUi.AddSection(root, "Full Page Contrast");
        ShowcaseUi.AddSubtitle(root,
            "WindowType.Full hides the Windows page — check Status in the lab strip, then dismiss to compare with overlays.");

        var back = ShowcaseUi.CreatePrimaryButton("Back to Windows");
        back.Pressed += () => RequireWindowManager().Dismiss(this);
        root.AddChild(back);
    }
}
