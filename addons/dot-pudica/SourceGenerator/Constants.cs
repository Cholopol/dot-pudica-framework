using System;
using System.Collections.Generic;

namespace DotPudica.SourceGenerator;

/// <summary>
/// Constants used by the source generator for symbol matching and control metadata.
/// </summary>
internal static class Constants
{
    public const string BindToAttributeFull = "DotPudica.Core.Binding.Attributes.BindToAttribute";
    public const string BindCommandAttributeFull = "DotPudica.Core.Binding.Attributes.BindCommandAttribute";
    public const string BindItemsAttributeFull = "DotPudica.Core.Binding.Attributes.BindItemsAttribute";
    public const string DotPudicaViewAttributeFull = "DotPudica.Godot.Views.DotPudicaViewAttribute";

    public const string BindToAttribute = "BindTo";
    public const string BindCommandAttribute = "BindCommand";
    public const string BindItemsAttribute = "BindItems";
    public const string DotPudicaViewAttribute = "DotPudicaView";

    public static readonly Dictionary<string, (string Property, string? Signal)> ControlDefaults
        = new(StringComparer.Ordinal)
    {
        ["Label"] = ("Text", null),
        ["RichTextLabel"] = ("Text", null),
        ["LineEdit"] = ("Text", "text_changed"),
        ["TextEdit"] = ("Text", "text_changed"),
        ["SpinBox"] = ("Value", "value_changed"),
        ["HSlider"] = ("Value", "value_changed"),
        ["VSlider"] = ("Value", "value_changed"),
        ["CheckBox"] = ("ButtonPressed", "toggled"),
        ["CheckButton"] = ("ButtonPressed", "toggled"),
        ["OptionButton"] = ("Selected", "item_selected"),
        ["ProgressBar"] = ("Value", null),
        ["TextureRect"] = ("Texture", null),
        ["Button"] = ("", "pressed"),
        ["LinkButton"] = ("", "pressed"),
    };
}
