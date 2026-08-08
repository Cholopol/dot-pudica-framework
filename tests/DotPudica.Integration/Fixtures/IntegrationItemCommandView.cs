using System.Windows.Input;
using DotPudica.Godot.Binding.ControlProxies;
using Godot;

namespace DotPudica.Integration.Fixtures;

/// <summary>ItemsSource row template with a select button that invokes the host-injected ItemCommand.</summary>
public partial class IntegrationItemCommandView : PanelContainer, IItemsControlItem, IItemsControlItemCommand
{
    private Label? _label;
    private Button? _button;
    private string? _item;

    public ICommand? ItemCommand { get; set; }

    public override void _Ready()
    {
        _label = GetNodeOrNull<Label>("Label");
        if (_label is null)
        {
            _label = new Label { Name = "Label" };
            AddChild(_label);
        }

        _button = GetNodeOrNull<Button>("SelectButton");
        if (_button is null)
        {
            _button = new Button { Name = "SelectButton", Text = "Select" };
            AddChild(_button);
        }

        // Idempotent subscription: _Ready can run more than once when the pooled row re-enters the tree.
        if (!_button.IsConnected(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnSelectPressed)))
            _button.Pressed += OnSelectPressed;
    }

    public object? DataContext
    {
        get => _item;
        set
        {
            _item = value?.ToString();
            if (_label is null)
                _Ready();
            if (_label is not null)
                _label.Text = _item ?? "";
        }
    }

    private void OnSelectPressed()
    {
        if (_item is null)
            return;
        if (ItemCommand?.CanExecute(_item) == true)
            ItemCommand.Execute(_item);
    }
}
