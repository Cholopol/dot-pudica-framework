using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace DotPudica.Integration.Fixtures;

public partial class DeclarativeBindingViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "initial";

    public int ClickCount { get; private set; }

    [RelayCommand]
    private void Click() => ClickCount++;
}

/// <summary>
/// Declarative user path golden fixture: uses source-generated BindTo / BindCommand instead of manually written TypedBindingPath.
/// </summary>
[DotPudicaView(typeof(DeclarativeBindingViewModel))]
public partial class DeclarativeBindingView : Control
{
    [Export, BindTo(nameof(DeclarativeBindingViewModel.Title), Mode = BindingMode.OneWay)]
    private Label _titleLabel = null!;

    [Export, BindCommand(nameof(DeclarativeBindingViewModel.ClickCommand))]
    private Button _clickButton = null!;

    public Label TitleLabel => _titleLabel;
    public Button ClickButton => _clickButton;
    public DeclarativeBindingViewModel? PanelViewModel => ViewModel;

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => DisposeView();

    partial void OnViewReady()
    {
        _titleLabel ??= new Label { Name = "TitleLabel" };
        if (_titleLabel.GetParent() is null)
            AddChild(_titleLabel);

        _clickButton ??= new Button { Name = "ClickButton", Text = "Click" };
        if (_clickButton.GetParent() is null)
            AddChild(_clickButton);
    }
}
