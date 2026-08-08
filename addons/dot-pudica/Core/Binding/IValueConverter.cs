namespace DotPudica.Core.Binding;

/// <summary>Typed converter; no boxing on the Convert hot path.</summary>
public interface IValueConverter<TIn, TOut>
{
    TOut Convert(TIn value);
    TIn ConvertBack(TOut value);
}

/// <summary>Type-erased converter for object pipelines and compatibility scenarios.</summary>
public interface IValueConverter
{
    object? Convert(object? value, Type targetType);
    object? ConvertBack(object? value, Type targetType);
}
