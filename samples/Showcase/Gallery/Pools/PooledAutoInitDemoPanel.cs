using DotPudica.Core.Binding.Attributes;
using DotPudica.Godot.Views;
using Godot;

namespace Samples.Showcase.Gallery.Pools;

/// <summary>Poolable auto-initialized MVVM panel ([DotPudicaView(Pooled = true)]): each tree entry re-runs _Ready → InitializeView (fresh Owned VM + bindings); _ExitTree → RecycleView recycles the node.</summary>
[DotPudicaView(typeof(PooledAutoInitDemoViewModel), Pooled = true)]
public partial class PooledAutoInitDemoPanel : VBoxContainer
{
    [Export, BindTo(nameof(PooledAutoInitDemoViewModel.Title))]
    private Label _titleLabel = null!;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => RecycleView();

    partial void OnViewReady()
    {
        _titleLabel ??= new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        if (_titleLabel.GetParent() is null)
            AddChild(_titleLabel);
    }
}
