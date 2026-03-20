using Godot;

namespace Samples.ReflectionProxy;

/// <summary>
/// Custom control without a dedicated proxy registration.
/// This forces DotPudica to use ReflectionProxy for binding.
/// </summary>
public partial class ReflectionProbeControl : VBoxContainer
{
    [Signal]
    public delegate void ValueTextChangedEventHandler();

    private readonly Label _valueLabel = new();
    private readonly Button _changeButton = new();
    private int _changeCount;
    private string _valueText = "Initial control text";

    public string ValueText
    {
        get => _valueText;
        set
        {
            if (_valueText == value)
                return;

            _valueText = value;
            _valueLabel.Text = $"Control text: {_valueText}";
            EmitSignal(SignalName.ValueTextChanged);
        }
    }

    public override void _Ready()
    {
        _valueLabel.Text = $"Control text: {_valueText}";
        AddChild(_valueLabel);

        _changeButton.Text = "Simulate Control Change";
        _changeButton.Pressed += OnChangeButtonPressed;
        AddChild(_changeButton);
    }

    public override void _ExitTree()
    {
        _changeButton.Pressed -= OnChangeButtonPressed;
        base._ExitTree();
    }

    private void OnChangeButtonPressed()
    {
        _changeCount++;
        ValueText = $"User change #{_changeCount}";
    }
}
