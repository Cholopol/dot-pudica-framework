using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Windows;

/// <summary>Fresh per-activation ViewModel for <see cref="DemoPooledPopupWindow"/> (Owned by the view, disposed on recycle).</summary>
public partial class DemoPooledPopupViewModel : ViewModelBase
{
    private static int _nextInstance;

    public DemoPooledPopupViewModel()
        => Title = $"Pooled popup (VM #{++_nextInstance})";

    [ObservableProperty]
    private string _title = "";
}

/// <summary>Pooled MVVM popup: Dismiss recycles the node into the manager pool; re-opening reuses it with a fresh ViewModel.</summary>
[DotPudicaView(typeof(DemoPooledPopupViewModel), Pooled = true)]
public partial class DemoPooledPopupWindow : GodotWindow
{
    [Export, BindTo(nameof(DemoPooledPopupViewModel.Title))]
    private Label _titleLabel = null!;

    public DemoPooledPopupWindow()
    {
        WindowType = WindowType.Popup;
        WindowName = "PooledPopup";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => RecycleView();

    protected override void OnCreate(IBundle? bundle)
    {
        // Self-attach mirroring ShowcasePageWindow.OnShow: OnCreate fires inside Create()
        // while the node is still parentless, so PrepareOverlay is a pure mount — never a
        // Reparent (which would trip _ExitTree → RecycleView and destroy the fresh VM).
        // Runs on every activation because ResetForReuse resets the lifecycle to Uncreated.
        ShowcaseLayout.PrepareOverlay(this);
        base.OnCreate(bundle);
    }

    partial void OnViewReady()
    {
        if (_titleLabel is not null)
            return;

        var content = DemoOverlayChrome.Build(this, new DemoOverlayChrome.PanelContent
        {
            Accent = DemoOverlayChrome.PopupAccent,
            Title = "Pooled Popup",
            Body = "Dismiss and re-open: the node is reused from the manager pool, the ViewModel is fresh.",
            PanelSize = new Vector2(340, 170),
            BackdropAlpha = 0.35f,
            BuildExtra = outer =>
            {
                _titleLabel = new Label
                {
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Modulate = ShowcaseTheme.Muted
                };
                outer.AddChild(_titleLabel);
            }
        });

        DemoOverlayChrome.AddCloseButton(content, "Close", () => Dismiss());
    }
}
