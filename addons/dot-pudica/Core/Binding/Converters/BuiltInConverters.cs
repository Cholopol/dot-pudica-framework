using System.Globalization;

namespace DotPudica.Core.Binding.Converters;

/// <summary>
/// Boolean negation converter. true ↔ false.
/// Commonly used in BindingMode.OneWay binding to convert IsLoading → button disabled.
/// </summary>
public class BoolNegateConverter : IValueConverter
{
    public static readonly BoolNegateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter)
        => value is bool b ? !b : value;
}

/// <summary>
/// Boolean to visibility converter (true = visible, false = invisible).
/// Corresponds to WPF's BooleanToVisibilityConverter, returns bool here for Godot's .Visible property.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    /// <param name="parameter">When non-null, indicates inversion (false = visible)</param>
    public object? Convert(object? value, Type targetType, object? parameter)
    {
        bool visible = value is bool b && b;
        return parameter != null ? !visible : visible;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter)
        => value is bool b && b;
}

/// <summary>
/// int → string converter.
/// </summary>
public class IntToStringConverter : IValueConverter
{
    public static readonly IntToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter)
    {
        if (value == null) return "";
        var format = parameter as string;
        return format != null
            ? ((IFormattable)value).ToString(format, CultureInfo.CurrentCulture)
            : value.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter)
    {
        if (int.TryParse(value?.ToString(), out var result))
            return result;
        return 0;
    }
}

/// <summary>
/// float → string converter, supports format strings.
/// </summary>
public class FloatToStringConverter : IValueConverter
{
    public static readonly FloatToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter)
    {
        if (value == null) return "";
        var format = parameter as string ?? "F2";
        return ((IFormattable)value).ToString(format, CultureInfo.CurrentCulture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter)
    {
        if (float.TryParse(value?.ToString(), out var result))
            return result;
        return 0f;
    }
}

/// <summary>
/// object → string converter (calls ToString()).
/// </summary>
public class ObjectToStringConverter : IValueConverter
{
    public static readonly ObjectToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter)
        => value?.ToString() ?? "";

    public object? ConvertBack(object? value, Type targetType, object? parameter)
        => value;
}

/// <summary>
/// string → bool converter (non-null and non-whitespace = true).
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public static readonly StringToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter)
        => !string.IsNullOrWhiteSpace(value as string);

    public object? ConvertBack(object? value, Type targetType, object? parameter)
        => value?.ToString() ?? "";
}

/// <summary>
/// Equality converter. Returns true when value equals parameter, commonly used for radio button binding to enums.
/// </summary>
public class EqualityConverter : IValueConverter
{
    public static readonly EqualityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter)
        => Equals(value, parameter);

    public object? ConvertBack(object? value, Type targetType, object? parameter)
        => value is bool b && b ? parameter : null;
}
