using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Pools;

/// <summary>
/// Poolable MVVM panel ([DotPudicaView(AutoInitialize = false, Pooled = true)]):
/// _ExitTree → RecycleView() recycles the node; BindShared re-activates it with a new ViewModel.
/// </summary>
[DotPudicaView(typeof(PooledDetailViewModel), AutoInitialize = false, Pooled = true)]
public partial class PooledDetailPanel : VBoxContainer
{
    [Export, BindTo(nameof(PooledDetailViewModel.Title))]
    private Label _titleLabel = null!;

    [Export, BindTo(nameof(PooledDetailViewModel.DetailText))]
    private Label _detailLabel = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => RecycleView();

    public void BindShared(PooledDetailViewModel vm) => ActivateViewModel(vm);

    partial void OnViewReady()
    {
        Name = "PooledDetail";
        AddThemeConstantOverride("separation", 4);

        _titleLabel ??= new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        if (_titleLabel.GetParent() is null)
            AddChild(_titleLabel);

        _detailLabel ??= new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = ShowcaseTheme.Muted
        };
        if (_detailLabel.GetParent() is null)
            AddChild(_detailLabel);
    }
}
