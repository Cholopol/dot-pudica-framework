using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace DotPudica.Integration.Fixtures;

/// <summary>Auto-initialized poolable ViewModel: Owned by the view, disposed on recycle.</summary>
public partial class PooledAutoInitViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "";
}

/// <summary>Poolable auto-initialized view ([DotPudicaView(Pooled = true)]): each tree entry re-runs _Ready → InitializeView (fresh Owned VM + bindings); _ExitTree → RecycleView recycles the node.</summary>
[DotPudicaView(typeof(PooledAutoInitViewModel), Pooled = true)]
public partial class PooledAutoInitView : Control
{
    [Export, BindTo(nameof(PooledAutoInitViewModel.Title), Mode = BindingMode.OneWay)]
    private Label _titleLabel = null!;

    public Label TitleLabel => _titleLabel;
    public PooledAutoInitViewModel? ViewVm => ViewModel;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => RecycleView();

    partial void OnViewReady()
    {
        _titleLabel ??= new Label { Name = "AutoInitTitle" };
        if (_titleLabel.GetParent() is null)
            AddChild(_titleLabel);
    }
}
