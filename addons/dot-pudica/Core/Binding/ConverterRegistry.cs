using System.Collections.Concurrent;

namespace DotPudica.Core.Binding;

/// <summary>
/// Manual converter instance registry. Generated code should reference converter singletons directly to avoid reflection activation.
/// </summary>
public static class ConverterRegistry
{
    private static readonly ConcurrentDictionary<Type, object> _cache = new();

    public static void Register<TConverter>(TConverter converter)
        where TConverter : class
        => _cache[typeof(TConverter)] = converter;

    public static bool TryGetTyped<TIn, TOut>(Type converterType, out IValueConverter<TIn, TOut>? converter)
    {
        ArgumentNullException.ThrowIfNull(converterType);
        if (_cache.TryGetValue(converterType, out var cached) && cached is IValueConverter<TIn, TOut> typed)
        {
            converter = typed;
            return true;
        }

        converter = null;
        return false;
    }

    public static bool TryGet(Type converterType, out IValueConverter? converter)
    {
        ArgumentNullException.ThrowIfNull(converterType);
        if (_cache.TryGetValue(converterType, out var cached) && cached is IValueConverter objectConverter)
        {
            converter = objectConverter;
            return true;
        }

        converter = null;
        return false;
    }

    /// <summary>Clear the cache (tests / ALC Reset).</summary>
    public static void Clear() => _cache.Clear();
}
