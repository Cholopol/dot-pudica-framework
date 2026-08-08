using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace DotPudica.Integration.Fixtures;

public partial class PooledWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "";
}

/// <summary>Poolable MVVM window: each activation creates a fresh Owned ViewModel; _ExitTree → RecycleView() recycles the node.</summary>
[DotPudicaView(typeof(PooledWindowViewModel), Pooled = true)]
public partial class PooledWindow : GodotWindow
{
    [Export, BindTo(nameof(PooledWindowViewModel.Title), Mode = BindingMode.OneWay)]
    private Label _titleLabel = null!;

    public Label TitleLabel => _titleLabel;
    public PooledWindowViewModel? WindowVm => ViewModel;

    public PooledWindow()
    {
        WindowType = WindowType.Popup;
        WindowName = "PooledWindow";
        CustomMinimumSize = new Vector2(80, 40);
    }

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => RecycleView();

    partial void OnViewReady()
    {
        _titleLabel ??= new Label { Name = "TitleLabel" };
        if (_titleLabel.GetParent() is null)
            AddChild(_titleLabel);
    }
}
