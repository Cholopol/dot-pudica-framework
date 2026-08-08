namespace DotPudica.Core.Binding;

/// <summary>Inclusive-exclusive source index window currently backed by item views.</summary>
public readonly record struct VirtualizedItemRange(int StartIndex, int EndIndex)
{
    public int Count => EndIndex - StartIndex;
}

public static class VirtualizedItemRangeCalculator
{
    public static VirtualizedItemRange Calculate(
        int itemCount,
        float itemHeight,
        float scrollOffset,
        float viewportHeight,
        int overscan = 1)
    {
        if (itemCount <= 0)
            return default;
        if (itemHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(itemHeight));
        if (viewportHeight < 0)
            throw new ArgumentOutOfRangeException(nameof(viewportHeight));
        if (overscan < 0)
            throw new ArgumentOutOfRangeException(nameof(overscan));

        var firstVisible = Math.Clamp((int)MathF.Floor(Math.Max(scrollOffset, 0) / itemHeight), 0, itemCount);
        var visibleCount = Math.Max(1, (int)MathF.Ceiling(viewportHeight / itemHeight));
        var start = Math.Max(0, firstVisible - overscan);
        var end = Math.Min(itemCount, firstVisible + visibleCount + overscan);
        return new VirtualizedItemRange(start, end);
    }
}
