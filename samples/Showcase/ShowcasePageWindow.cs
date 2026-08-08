using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace Samples.Showcase;

/// <summary>Showcase navigation page base class: Full window, driven by GodotWindowManager for switching.</summary>
public abstract partial class ShowcasePageWindow : GodotWindow
{
    protected ShowcasePageWindow()
    {
        WindowType = WindowType.Full;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var bg = new ColorRect
        {
            Name = "PageBackground",
            Color = ShowcaseTheme.PageBg,
            MouseFilter = MouseFilterEnum.Ignore
        };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);
    }

    protected override void OnShow()
    {
        if (GetParent() == null)
            ShowcaseLayout.AttachFullPage(this);
        if (!string.IsNullOrEmpty(WindowName))
            ShowcaseLayout.SetBannerInstance(WindowName);
        var bg = GetNodeOrNull<ColorRect>("PageBackground");
        if (bg is not null)
            MoveChild(bg, 0);
        base.OnShow();
    }

    protected static GodotWindowManager RequireWindowManager()
        => AppContext.Current.WindowManager;

    /// <summary>Attach to ContentHost first, then Show, to avoid Reparent removing from the tree.</summary>
    protected static void ShowFullPage(GodotWindow page)
    {
        ShowcaseLayout.PrepareFullPage(page);
        RequireWindowManager().Show(page);
    }
}
