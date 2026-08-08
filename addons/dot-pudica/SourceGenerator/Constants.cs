using System.Collections.Generic;

namespace DotPudica.SourceGenerator;

internal static class Constants
{
    public const string BindToAttributeFull = "DotPudica.Core.Binding.Attributes.BindToAttribute";
    public const string BindCommandAttributeFull = "DotPudica.Core.Binding.Attributes.BindCommandAttribute";
    public const string ItemsSourceAttributeFull = "DotPudica.Core.Binding.Attributes.ItemsSourceAttribute";
    public const string DotPudicaViewAttributeFull = "DotPudica.Godot.Views.DotPudicaViewAttribute";
    public const string InjectAttributeFull = "DotPudica.Core.Composition.InjectAttribute";
    public const string ViewModelFactoryAttributeFull = "DotPudica.Core.Composition.ViewModelFactoryAttribute";
    public const string SubscribeAttributeFull = "DotPudica.Core.Composition.SubscribeAttribute";

    public const string BindToAttribute = "BindTo";
    public const string BindCommandAttribute = "BindCommand";
    public const string ItemsSourceAttribute = "ItemsSource";
    public const string DotPudicaViewAttribute = "DotPudicaView";
    public const string InjectAttribute = "Inject";
    public const string ViewModelFactoryAttribute = "ViewModelFactory";
    public const string SubscribeAttribute = "Subscribe";

    public const string VirtualizedItemsControlTypeName = "VirtualizedItemsControl";

    public static readonly Dictionary<string, (string Property, string? Signal)> ControlDefaults
        = new(System.StringComparer.Ordinal)
        {
            ["Label"] = ("Text", null),
            ["RichTextLabel"] = ("Text", null),
            ["LineEdit"] = ("Text", "text_changed"),
            ["TextEdit"] = ("Text", "text_changed"),
            ["SpinBox"] = ("Value", "value_changed"),
            ["HSlider"] = ("Value", "value_changed"),
            ["VSlider"] = ("Value", "value_changed"),
            ["Slider"] = ("Value", "value_changed"),
            ["CheckBox"] = ("ButtonPressed", "toggled"),
            ["CheckButton"] = ("ButtonPressed", "toggled"),
            ["OptionButton"] = ("Selected", "item_selected"),
            ["ProgressBar"] = ("Value", null),
            ["TextureRect"] = ("Texture", null),
            ["Button"] = ("", "pressed"),
            ["LinkButton"] = ("", "pressed"),
            ["BaseButton"] = ("", "pressed"),
        };

    public static readonly Dictionary<string, string> CommandSignals
        = new(System.StringComparer.Ordinal)
        {
            ["Button"] = "pressed",
            ["LinkButton"] = "pressed",
            ["BaseButton"] = "pressed",
        };

    public static readonly Dictionary<string, string> BuiltInProxyTypes
        = new(System.StringComparer.Ordinal)
        {
            ["Label"] = "LabelProxy",
            ["RichTextLabel"] = "RichTextLabelProxy",
            ["LineEdit"] = "LineEditProxy",
            ["TextEdit"] = "TextEditProxy",
            ["SpinBox"] = "SpinBoxProxy",
            ["HSlider"] = "SliderProxy",
            ["VSlider"] = "SliderProxy",
            ["Slider"] = "SliderProxy",
            ["CheckBox"] = "CheckBoxProxy",
            ["CheckButton"] = "CheckBoxProxy",
            ["OptionButton"] = "OptionButtonProxy",
            ["ProgressBar"] = "ProgressBarProxy",
            ["TextureRect"] = "TextureRectProxy",
        };

    public static readonly string[] RangeBindingTargetNames =
    [
        "Value",
        "MinValue",
        "MaxValue"
    ];

    public static readonly string[] GodotRangeTypeNameSuffixes =
    [
        "ProgressBar",
        "Slider",
        "HSlider",
        "VSlider",
        "SpinBox",
        "Range"
    ];

    // Built-in proxies ignore Target= by control type; unsupported targets fall back to DelegateTargetProxy.
    public static readonly Dictionary<string, string[]> BuiltInProxySupportedTargets
        = new(System.StringComparer.Ordinal)
        {
            ["Label"] = new[] { "Text" },
            ["RichTextLabel"] = new[] { "Text", "BbcodeText" },
            ["LineEdit"] = new[] { "Text" },
            ["TextEdit"] = new[] { "Text" },
            ["SpinBox"] = RangeBindingTargetNames,
            ["HSlider"] = RangeBindingTargetNames,
            ["VSlider"] = RangeBindingTargetNames,
            ["Slider"] = RangeBindingTargetNames,
            ["CheckBox"] = new[] { "ButtonPressed" },
            ["CheckButton"] = new[] { "ButtonPressed" },
            ["OptionButton"] = new[] { "Selected" },
            ["ProgressBar"] = RangeBindingTargetNames,
            ["TextureRect"] = new[] { "Texture" },
        };
}
