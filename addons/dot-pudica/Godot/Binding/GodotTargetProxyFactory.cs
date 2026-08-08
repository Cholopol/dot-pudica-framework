using DotPudica.Core.Binding;
using DotPudica.Godot.Binding;
using DotPudica.Godot.Binding.ControlProxies;
using Godot;

namespace DotPudica.Godot;

/// <summary>
/// Unknown controls should use <see cref="DelegateTargetProxy{TControl,TValue}"/> emitted by the source generator, no reflection.
/// </summary>
public static class GodotTargetProxyFactory
{
	private static readonly Dictionary<Type, Func<Control, string, string?, ITargetProxy>> _customFactories = new();

	public static void Register<TControl>(Func<TControl, string, string?, ITargetProxy> factory)
		where TControl : Control
	{
		_customFactories[typeof(TControl)] = (ctrl, prop, evt) => factory((TControl)ctrl, prop, evt);
	}

	/// <summary>Clears custom factories to prevent being pinned by Type/delegates during ALC unload.</summary>
	public static void Clear() => _customFactories.Clear();

	public static ITargetProxy Create(Control control, string targetProperty, string? changeSignal)
	{
		if (_customFactories.TryGetValue(control.GetType(), out var factory))
			return factory(control, targetProperty, changeSignal);

		return control switch
		{
			Label label => new LabelProxy(label),
			RichTextLabel richLabel => new RichTextLabelProxy(richLabel,
				targetProperty.Equals("BbcodeText", StringComparison.OrdinalIgnoreCase)),
			LineEdit lineEdit => new LineEditProxy(lineEdit),
			TextEdit textEdit => new TextEditProxy(textEdit),
			SpinBox spinBox => new SpinBoxProxy(spinBox, ResolveRangeProperty(targetProperty)),
			CheckBox checkBox => new CheckBoxProxy(checkBox),
			CheckButton checkButton => new CheckBoxProxy(checkButton),
			OptionButton optionButton => new OptionButtonProxy(optionButton),
			ProgressBar progressBar => new ProgressBarProxy(progressBar, ResolveRangeProperty(targetProperty)),
			TextureRect textureRect => new TextureRectProxy(textureRect),
			Slider slider => new SliderProxy(slider, ResolveRangeProperty(targetProperty)),
			_ => throw new InvalidOperationException(
				$"Control type {control.GetType().FullName} has no dedicated proxy." +
				"Please bind via source generator (which will emit a DelegateTargetProxy), or call GodotTargetProxyFactory.Register to register a custom factory.")
		};
	}

	private static RangeBindingProperty ResolveRangeProperty(string targetProperty)
		=> GodotRangeBinding.TryParseProperty(targetProperty, out var property)
			? property
			: RangeBindingProperty.Value;
}

/// <summary>
/// Delegate-driven target proxy. Emitted by the source generator for any control property, AOT/trimming safe.
/// </summary>
public sealed class DelegateTargetProxy<TControl, TValue> : ITypedTargetProxy<TValue>
	where TControl : Control
{
	private readonly TControl _control;
	private readonly Func<TControl, TValue> _getter;
	private readonly Action<TControl, TValue>? _setter;
	private readonly string? _changeSignal;
	private Callable? _callable;

	public event EventHandler? ValueChanged;

	public DelegateTargetProxy(
		TControl control,
		Func<TControl, TValue> getter,
		Action<TControl, TValue>? setter = null,
		string? changeSignal = null)
	{
		_control = control ?? throw new ArgumentNullException(nameof(control));
		_getter = getter ?? throw new ArgumentNullException(nameof(getter));
		_setter = setter;
		_changeSignal = changeSignal;

		if (changeSignal is not null)
		{
			_callable = Callable.From(() => ValueChanged?.Invoke(this, EventArgs.Empty));
			_control.Connect(changeSignal, _callable.Value);
		}
	}

	public TValue GetValue() => _getter(_control);

	public void SetValue(TValue value)
	{
		if (_setter is null)
			return;
		_setter(_control, value);
	}

	public void Dispose()
	{
		if (_callable is { } callable && _changeSignal is not null
			&& GodotObject.IsInstanceValid(_control)
			&& _control.IsConnected(_changeSignal, callable))
		{
			_control.Disconnect(_changeSignal, callable);
		}

		_callable = null;
		ValueChanged = null;
	}
}
