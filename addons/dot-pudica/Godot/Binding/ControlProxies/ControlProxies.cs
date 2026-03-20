using DotPudica.Core.Binding;
using Godot;

namespace DotPudica.Godot.Binding.ControlProxies;

/// <summary>
/// Label control binding proxy (read-only, no change event).
/// </summary>
public class LabelProxy : ITargetProxy
{
    private readonly Label _label;
    public Type TargetType => typeof(string);
    event EventHandler? ITargetProxy.ValueChanged
    {
        add { }
        remove { }
    }

    public LabelProxy(Label label) => _label = label;

    public object? GetValue() => _label.Text;
    public void SetValue(object? value) => _label.Text = value?.ToString() ?? "";
    public void Dispose() { }
}

/// <summary>
/// RichTextLabel control binding proxy.
/// </summary>
public class RichTextLabelProxy : ITargetProxy
{
    private readonly RichTextLabel _label;
    private readonly bool _useBbcode;
    public Type TargetType => typeof(string);
    event EventHandler? ITargetProxy.ValueChanged
    {
        add { }
        remove { }
    }

    public RichTextLabelProxy(RichTextLabel label, bool useBbcode = false)
    {
        _label = label;
        _useBbcode = useBbcode;
    }

    public object? GetValue() => _useBbcode ? _label.Text : _label.Text;
    public void SetValue(object? value)
    {
        var text = value?.ToString() ?? "";
        if (_useBbcode)
        {
            _label.BbcodeEnabled = true;
            _label.Text = text;
        }
        else
        {
            _label.Text = text;
        }
    }
    public void Dispose() { }
}

/// <summary>
/// LineEdit control binding proxy (supports two-way binding).
/// </summary>
public class LineEditProxy : ITargetProxy
{
    private readonly LineEdit _lineEdit;
    public Type TargetType => typeof(string);
    public event EventHandler? ValueChanged;

    public LineEditProxy(LineEdit lineEdit)
    {
        _lineEdit = lineEdit;
        _lineEdit.TextChanged += OnTextChanged;
    }

    private void OnTextChanged(string newText) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public object? GetValue() => _lineEdit.Text;
    public void SetValue(object? value) => _lineEdit.Text = value?.ToString() ?? "";

    public void Dispose() => _lineEdit.TextChanged -= OnTextChanged;
}

/// <summary>
/// TextEdit control binding proxy (supports two-way binding).
/// </summary>
public class TextEditProxy : ITargetProxy
{
    private readonly TextEdit _textEdit;
    private Callable? _callable;
    public Type TargetType => typeof(string);
    public event EventHandler? ValueChanged;

    public TextEditProxy(TextEdit textEdit)
    {
        _textEdit = textEdit;
        _callable = Callable.From(OnTextChanged);
        _textEdit.Connect("text_changed", _callable.Value);
    }

    private void OnTextChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    public object? GetValue() => _textEdit.Text;
    public void SetValue(object? value) => _textEdit.Text = value?.ToString() ?? "";

    public void Dispose()
    {
        if (_callable.HasValue)
            _textEdit.Disconnect("text_changed", _callable.Value);
    }
}

/// <summary>
/// CheckBox / CheckButton control binding proxy (supports two-way binding).
/// </summary>
public class CheckBoxProxy : ITargetProxy
{
    private readonly BaseButton _button;
    private Callable? _callable;
    public Type TargetType => typeof(bool);
    public event EventHandler? ValueChanged;

    public CheckBoxProxy(BaseButton button)
    {
        _button = button;
        _callable = Callable.From<bool>(OnToggled);
        _button.Connect("toggled", _callable.Value);
    }

    private void OnToggled(bool pressed) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public object? GetValue() => _button.ButtonPressed;
    public void SetValue(object? value)
    {
        if (value is bool b)
            _button.ButtonPressed = b;
    }

    public void Dispose()
    {
        if (_callable.HasValue)
            _button.Disconnect("toggled", _callable.Value);
    }
}

/// <summary>
/// SpinBox control binding proxy (supports two-way binding).
/// </summary>
public class SpinBoxProxy : ITargetProxy
{
    private readonly SpinBox _spinBox;
    private Callable? _callable;
    public Type TargetType => typeof(double);
    public event EventHandler? ValueChanged;

    public SpinBoxProxy(SpinBox spinBox)
    {
        _spinBox = spinBox;
        _callable = Callable.From<double>(OnValueChanged);
        _spinBox.Connect("value_changed", _callable.Value);
    }

    private void OnValueChanged(double value) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public object? GetValue() => _spinBox.Value;
    public void SetValue(object? value)
    {
        if (value is double d)
            _spinBox.Value = d;
        else if (value != null)
            _spinBox.Value = System.Convert.ToDouble(value);
    }

    public void Dispose()
    {
        if (_callable.HasValue)
            _spinBox.Disconnect("value_changed", _callable.Value);
    }
}

/// <summary>
/// HSlider / VSlider control binding proxy (supports two-way binding).
/// </summary>
public class SliderProxy : ITargetProxy
{
    private readonly Slider _slider;
    private Callable? _callable;
    public Type TargetType => typeof(double);
    public event EventHandler? ValueChanged;

    public SliderProxy(Slider slider)
    {
        _slider = slider;
        _callable = Callable.From<double>(OnValueChanged);
        _slider.Connect("value_changed", _callable.Value);
    }

    private void OnValueChanged(double value) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public object? GetValue() => _slider.Value;
    public void SetValue(object? value)
    {
        if (value is double d)
            _slider.Value = d;
        else if (value != null)
            _slider.Value = System.Convert.ToDouble(value);
    }

    public void Dispose()
    {
        if (_callable.HasValue)
            _slider.Disconnect("value_changed", _callable.Value);
    }
}

/// <summary>
/// OptionButton control binding proxy.
/// </summary>
public class OptionButtonProxy : ITargetProxy
{
    private readonly OptionButton _optionButton;
    private Callable? _callable;
    public Type TargetType => typeof(int);
    public event EventHandler? ValueChanged;

    public OptionButtonProxy(OptionButton optionButton)
    {
        _optionButton = optionButton;
        _callable = Callable.From<long>(OnItemSelected);
        _optionButton.Connect("item_selected", _callable.Value);
    }

    private void OnItemSelected(long index) => ValueChanged?.Invoke(this, EventArgs.Empty);

    public object? GetValue() => _optionButton.Selected;
    public void SetValue(object? value)
    {
        if (value is int i)
            _optionButton.Selected = i;
    }

    public void Dispose()
    {
        if (_callable.HasValue)
            _optionButton.Disconnect("item_selected", _callable.Value);
    }
}

/// <summary>
/// ProgressBar control binding proxy (read-only).
/// </summary>
public class ProgressBarProxy : ITargetProxy
{
    private readonly ProgressBar _progressBar;
    public Type TargetType => typeof(double);
    event EventHandler? ITargetProxy.ValueChanged
    {
        add { }
        remove { }
    }

    public ProgressBarProxy(ProgressBar progressBar) => _progressBar = progressBar;

    public object? GetValue() => _progressBar.Value;
    public void SetValue(object? value)
    {
        if (value is double d)
            _progressBar.Value = d;
        else if (value != null)
            _progressBar.Value = System.Convert.ToDouble(value);
    }
    public void Dispose() { }
}

/// <summary>
/// TextureRect control binding proxy.
/// </summary>
public class TextureRectProxy : ITargetProxy
{
    private readonly TextureRect _textureRect;
    public Type TargetType => typeof(Texture2D);
    event EventHandler? ITargetProxy.ValueChanged
    {
        add { }
        remove { }
    }

    public TextureRectProxy(TextureRect textureRect) => _textureRect = textureRect;

    public object? GetValue() => _textureRect.Texture;
    public void SetValue(object? value)
    {
        if (value is Texture2D texture)
            _textureRect.Texture = texture;
    }
    public void Dispose() { }
}
