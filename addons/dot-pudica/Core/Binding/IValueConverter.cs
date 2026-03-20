namespace DotPudica.Core.Binding;

/// <summary>
/// Value converter interface, used to convert source and target values during binding.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Convert source value (ViewModel) to target value (View control).
    /// </summary>
    object? Convert(object? value, Type targetType, object? parameter);

    /// <summary>
    /// Convert target value (View control) back to source value (ViewModel), used for TwoWay binding.
    /// </summary>
    object? ConvertBack(object? value, Type targetType, object? parameter);
}
