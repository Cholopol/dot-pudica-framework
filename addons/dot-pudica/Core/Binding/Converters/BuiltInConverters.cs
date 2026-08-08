using System.Globalization;

namespace DotPudica.Core.Binding.Converters;

public class BoolNegateConverter : IValueConverter, IValueConverter<bool, bool>
{
    public static readonly BoolNegateConverter Instance = new();

    public object? Convert(object? value, Type targetType)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType)
        => value is bool b ? !b : value;

    bool IValueConverter<bool, bool>.Convert(bool value) => !value;

    bool IValueConverter<bool, bool>.ConvertBack(bool value) => !value;
}

/// <summary>Named pass-through for Godot <c>Visible</c> (already bool).</summary>
public class BoolToVisibilityConverter : IValueConverter, IValueConverter<bool, bool>
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType)
        => value is bool b && b;

    public object? ConvertBack(object? value, Type targetType)
        => value is bool b && b;

    bool IValueConverter<bool, bool>.Convert(bool value) => value;

    bool IValueConverter<bool, bool>.ConvertBack(bool value) => value;
}

public class IntToStringConverter : IValueConverter, IValueConverter<int, string>
{
    public static readonly IntToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType)
        => value?.ToString() ?? "";

    public object? ConvertBack(object? value, Type targetType)
        => int.TryParse(value?.ToString(), out var result) ? result : 0;

    string IValueConverter<int, string>.Convert(int value)
        => value.ToString(CultureInfo.CurrentCulture);

    int IValueConverter<int, string>.ConvertBack(string value)
        => int.TryParse(value, out var result) ? result : 0;
}

/// <summary>Formats with fixed two decimal places.</summary>
public class FloatToStringConverter : IValueConverter, IValueConverter<float, string>
{
    public static readonly FloatToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType)
    {
        if (value == null) return "";
        return ((IFormattable)value).ToString("F2", CultureInfo.CurrentCulture);
    }

    public object? ConvertBack(object? value, Type targetType)
        => float.TryParse(value?.ToString(), out var result) ? result : 0f;

    string IValueConverter<float, string>.Convert(float value)
        => value.ToString("F2", CultureInfo.CurrentCulture);

    float IValueConverter<float, string>.ConvertBack(string value)
        => float.TryParse(value, out var result) ? result : 0f;
}

/// <summary>
/// Non-nullable <c>object</c>/<c>string</c> typed sides match typical VM property types for BindProperty;
/// null is still handled on the untyped path.
/// </summary>
public class ObjectToStringConverter : IValueConverter, IValueConverter<object, string>
{
    public static readonly ObjectToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType)
        => value?.ToString() ?? "";

    public object? ConvertBack(object? value, Type targetType)
        => value;

    string IValueConverter<object, string>.Convert(object value)
        => value?.ToString() ?? "";

    object IValueConverter<object, string>.ConvertBack(string value)
        => value;
}

/// <summary>
/// Non-null/non-whitespace → true. Non-nullable typed sides match typical VM property types for BindProperty.
/// </summary>
public class StringToBoolConverter : IValueConverter, IValueConverter<string, bool>
{
    public static readonly StringToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType)
        => !string.IsNullOrWhiteSpace(value as string);

    public object? ConvertBack(object? value, Type targetType)
        => value?.ToString() ?? "";

    bool IValueConverter<string, bool>.Convert(string value)
        => !string.IsNullOrWhiteSpace(value);

    string IValueConverter<string, bool>.ConvertBack(bool value)
        => value.ToString();
}
