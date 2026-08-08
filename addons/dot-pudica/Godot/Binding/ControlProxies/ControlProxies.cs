using DotPudica.Core.Binding;
using DotPudica.Godot.Binding;
using Godot;

namespace DotPudica.Godot.Binding.ControlProxies;

public class LabelProxy : ITypedTargetProxy<string>, ITargetProxy
{
    private readonly Label _label;

    event EventHandler? ITypedTargetProxy<string>.ValueChanged
    {
        add { }
        remove { }
    }

    event EventHandler? ITypedTargetProxy<object?>.ValueChanged
    {
        add { }
        remove { }
    }

    public LabelProxy(Label label) => _label = label;

    public string GetValue() => _label.Text;
    public void SetValue(string value) => _label.Text = value;

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();
    void ITypedTargetProxy<object?>.SetValue(object? value) => SetValue(value?.ToString() ?? "");

    public void Dispose() { }
}

public class RichTextLabelProxy : ITypedTargetProxy<string>, ITargetProxy
{
    private readonly RichTextLabel _label;
    private readonly bool _useBbcode;

    event EventHandler? ITypedTargetProxy<string>.ValueChanged
    {
        add { }
        remove { }
    }

    event EventHandler? ITypedTargetProxy<object?>.ValueChanged
    {
        add { }
        remove { }
    }

    public RichTextLabelProxy(RichTextLabel label, bool useBbcode = false)
    {
        _label = label;
        _useBbcode = useBbcode;
    }

    public string GetValue() => _label.Text;

    public void SetValue(string value)
    {
        if (_useBbcode)
            _label.BbcodeEnabled = true;
        _label.Text = value;
    }

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();
    void ITypedTargetProxy<object?>.SetValue(object? value) => SetValue(value?.ToString() ?? "");

    public void Dispose() { }
}

public class LineEditProxy : ITypedTargetProxy<string>, ITargetProxy
{
    private readonly LineEdit _lineEdit;
    public event EventHandler? ValueChanged;

    public LineEditProxy(LineEdit lineEdit)
    {
        _lineEdit = lineEdit;
        _lineEdit.TextChanged += OnTextChanged;
    }

    private void OnTextChanged(string newText) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public string GetValue() => _lineEdit.Text;
    public void SetValue(string value) => _lineEdit.Text = value;

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();
    void ITypedTargetProxy<object?>.SetValue(object? value) => SetValue(value?.ToString() ?? "");

    public void Dispose()
    {
        ValueChanged = null;
        if (GodotObject.IsInstanceValid(_lineEdit))
            _lineEdit.TextChanged -= OnTextChanged;
    }
}

public class TextEditProxy : ITypedTargetProxy<string>, ITargetProxy
{
    private readonly TextEdit _textEdit;
    private Callable? _callable;
    public event EventHandler? ValueChanged;

    public TextEditProxy(TextEdit textEdit)
    {
        _textEdit = textEdit;
        _callable = Callable.From(OnTextChanged);
        _textEdit.Connect("text_changed", _callable.Value);
    }

    private void OnTextChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    public string GetValue() => _textEdit.Text;
    public void SetValue(string value) => _textEdit.Text = value;

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();
    void ITypedTargetProxy<object?>.SetValue(object? value) => SetValue(value?.ToString() ?? "");

    public void Dispose()
    {
        if (_callable is { } callable
            && GodotObject.IsInstanceValid(_textEdit)
            && _textEdit.IsConnected("text_changed", callable))
        {
            _textEdit.Disconnect("text_changed", callable);
        }

        _callable = null;
        ValueChanged = null;
    }
}

public class CheckBoxProxy : ITypedTargetProxy<bool>, ITargetProxy
{
    private readonly BaseButton _button;
    private Callable? _callable;
    public event EventHandler? ValueChanged;

    public CheckBoxProxy(BaseButton button)
    {
        _button = button;
        _callable = Callable.From<bool>(OnToggled);
        _button.Connect("toggled", _callable.Value);
    }

    private void OnToggled(bool pressed) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public bool GetValue() => _button.ButtonPressed;

    public void SetValue(bool value) => _button.ButtonPressed = value;

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();

    void ITypedTargetProxy<object?>.SetValue(object? value)
    {
        if (value is bool b)
            SetValue(b);
    }

    public void Dispose()
    {
        if (_callable is { } callable
            && GodotObject.IsInstanceValid(_button)
            && _button.IsConnected("toggled", callable))
        {
            _button.Disconnect("toggled", callable);
        }

        _callable = null;
        ValueChanged = null;
    }
}

/// <summary>
/// Min/Max/Value are all written through <see cref="GodotRangeBinding"/> for coordinated updates.
/// Two-way binding only on <see cref="RangeBindingProperty.Value"/>.
/// </summary>
public class SpinBoxProxy : ITypedTargetProxy<double>, ITargetProxy
{
    private readonly SpinBox _spinBox;
    private readonly RangeBindingProperty _property;
    private Callable? _callable;
    public event EventHandler? ValueChanged;

    public SpinBoxProxy(SpinBox spinBox, RangeBindingProperty property = RangeBindingProperty.Value)
    {
        _spinBox = spinBox;
        _property = property;
        if (property == RangeBindingProperty.Value)
        {
            _callable = Callable.From<double>(OnValueChanged);
            _spinBox.Connect("value_changed", _callable.Value);
        }
    }

    private void OnValueChanged(double value) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public double GetValue() => GodotRangeBinding.GetProperty(_spinBox, _property);

    public void SetValue(double value) => GodotRangeBinding.SetProperty(_spinBox, _property, value);

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();

    void ITypedTargetProxy<object?>.SetValue(object? value)
    {
        if (value is double d)
            SetValue(d);
        else if (value != null)
            SetValue(Convert.ToDouble(value));
    }

    public void Dispose()
    {
        if (_callable is { } callable
            && GodotObject.IsInstanceValid(_spinBox)
            && _spinBox.IsConnected("value_changed", callable))
        {
            _spinBox.Disconnect("value_changed", callable);
        }

        _callable = null;
        ValueChanged = null;
    }
}

/// <summary>
/// Min/Max/Value are all written through <see cref="GodotRangeBinding"/> for coordinated updates.
/// Two-way binding only on <see cref="RangeBindingProperty.Value"/>.
/// </summary>
public class SliderProxy : ITypedTargetProxy<double>, ITargetProxy
{
    private readonly Slider _slider;
    private readonly RangeBindingProperty _property;
    private Callable? _callable;
    public event EventHandler? ValueChanged;

    public SliderProxy(Slider slider, RangeBindingProperty property = RangeBindingProperty.Value)
    {
        _slider = slider;
        _property = property;
        if (property == RangeBindingProperty.Value)
        {
            _callable = Callable.From<double>(OnValueChanged);
            _slider.Connect("value_changed", _callable.Value);
        }
    }

    private void OnValueChanged(double value) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public double GetValue() => GodotRangeBinding.GetProperty(_slider, _property);

    public void SetValue(double value) => GodotRangeBinding.SetProperty(_slider, _property, value);

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();

    void ITypedTargetProxy<object?>.SetValue(object? value)
    {
        if (value is double d)
            SetValue(d);
        else if (value != null)
            SetValue(Convert.ToDouble(value));
    }

    public void Dispose()
    {
        if (_callable is { } callable
            && GodotObject.IsInstanceValid(_slider)
            && _slider.IsConnected("value_changed", callable))
        {
            _slider.Disconnect("value_changed", callable);
        }

        _callable = null;
        ValueChanged = null;
    }
}

public class OptionButtonProxy : ITypedTargetProxy<int>, ITargetProxy
{
    private readonly OptionButton _optionButton;
    private Callable? _callable;
    public event EventHandler? ValueChanged;

    public OptionButtonProxy(OptionButton optionButton)
    {
        _optionButton = optionButton;
        _callable = Callable.From<long>(OnItemSelected);
        _optionButton.Connect("item_selected", _callable.Value);
    }

    private void OnItemSelected(long index) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public int GetValue() => _optionButton.Selected;

    public void SetValue(int value) => _optionButton.Selected = value;

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();

    void ITypedTargetProxy<object?>.SetValue(object? value)
    {
        if (value is int i)
            SetValue(i);
    }

    public void Dispose()
    {
        if (_callable is { } callable
            && GodotObject.IsInstanceValid(_optionButton)
            && _optionButton.IsConnected("item_selected", callable))
        {
            _optionButton.Disconnect("item_selected", callable);
        }

        _callable = null;
        ValueChanged = null;
    }
}

/// <summary>
/// Default OneWay; Min/Max/Value coordinated through <see cref="GodotRangeBinding"/>.
/// </summary>
public class ProgressBarProxy : ITypedTargetProxy<double>, ITargetProxy
{
    private readonly ProgressBar _progressBar;
    private readonly RangeBindingProperty _property;

    event EventHandler? ITypedTargetProxy<double>.ValueChanged
    {
        add { }
        remove { }
    }

    event EventHandler? ITypedTargetProxy<object?>.ValueChanged
    {
        add { }
        remove { }
    }

    public ProgressBarProxy(ProgressBar progressBar, RangeBindingProperty property = RangeBindingProperty.Value)
    {
        _progressBar = progressBar;
        _property = property;
    }

    public double GetValue() => GodotRangeBinding.GetProperty(_progressBar, _property);

    public void SetValue(double value) => GodotRangeBinding.SetProperty(_progressBar, _property, value);

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();

    void ITypedTargetProxy<object?>.SetValue(object? value)
    {
        if (value is double d)
            SetValue(d);
        else if (value != null)
            SetValue(Convert.ToDouble(value));
    }

    public void Dispose() { }
}

public class TextureRectProxy : ITypedTargetProxy<Texture2D?>, ITargetProxy
{
    private readonly TextureRect _textureRect;

    event EventHandler? ITypedTargetProxy<Texture2D?>.ValueChanged
    {
        add { }
        remove { }
    }

    event EventHandler? ITypedTargetProxy<object?>.ValueChanged
    {
        add { }
        remove { }
    }

    public TextureRectProxy(TextureRect textureRect) => _textureRect = textureRect;

    public Texture2D? GetValue() => _textureRect.Texture;

    public void SetValue(Texture2D? value) => _textureRect.Texture = value;

    object? ITypedTargetProxy<object?>.GetValue() => GetValue();

    void ITypedTargetProxy<object?>.SetValue(object? value)
    {
        if (value is Texture2D texture)
            SetValue(texture);
        else if (value is null)
            SetValue(null);
    }

    public void Dispose() { }
}
