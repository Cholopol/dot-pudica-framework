using DotPudica.Godot.Binding.ControlProxies;
using Godot;

namespace DotPudica.Integration.Fixtures;

/// <summary>Minimal ItemsSource row template for integration tests.</summary>
public partial class IntegrationItemView : PanelContainer, IItemsControlItem
{
    private Label? _label;

    public override void _Ready()
    {
        _label = GetNodeOrNull<Label>("Label");
        if (_label is null)
        {
            _label = new Label { Name = "Label" };
            AddChild(_label);
        }
    }

    public object? DataContext
    {
        get => _label?.Text;
        set
        {
            if (_label is null)
                _Ready();
            if (_label is not null)
                _label.Text = value?.ToString() ?? "";
        }
    }
}
