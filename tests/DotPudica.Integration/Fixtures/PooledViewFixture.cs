using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Attributes;
using DotPudica.Core.Composition;
using DotPudica.Core.ViewModels;
using DotPudica.Godot.Views;
using Godot;

namespace DotPudica.Integration.Fixtures;

/// <summary>Poolable item ViewModel: externally owned, disposed by the holder, never by the pooled view.</summary>
public partial class PooledItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "";

    public int ClickCount { get; private set; }

    /// <summary>Event wired by [Subscribe]; verifies subscription re-wiring across activation cycles.</summary>
    public event Action? Ping;

    public void RaisePing() => Ping?.Invoke();

    [RelayCommand]
    private void Click() => ClickCount++;
}

/// <summary>
/// Poolable declarative view ([DotPudicaView(AutoInitialize = false, Pooled = true)]):
/// _ExitTree → RecycleView() recycles the node instead of disposing it; ActivateViewModel re-binds.
/// </summary>
[DotPudicaView(typeof(PooledItemViewModel), AutoInitialize = false, Pooled = true)]
public partial class PooledItemView : Control
{
    [Export, BindTo(nameof(PooledItemViewModel.Title), Mode = BindingMode.OneWay)]
    private Label _titleLabel = null!;

    [Export, BindCommand(nameof(PooledItemViewModel.ClickCommand))]
    private Button _clickButton = null!;

    public Label TitleLabel => _titleLabel;
    public Button ClickButton => _clickButton;

    public int PingCount { get; private set; }

    public override void _Ready() => InitializeView();

    public override void _ExitTree() => RecycleView();

    public void BindShared(PooledItemViewModel shared) => ActivateViewModel(shared);

    partial void OnViewReady()
    {
        _titleLabel ??= new Label { Name = "TitleLabel" };
        if (_titleLabel.GetParent() is null)
            AddChild(_titleLabel);

        _clickButton ??= new Button { Name = "ClickButton", Text = "Click" };
        if (_clickButton.GetParent() is null)
            AddChild(_clickButton);
    }

    [Subscribe("Ping")]
    private void OnPing() => PingCount++;
}
