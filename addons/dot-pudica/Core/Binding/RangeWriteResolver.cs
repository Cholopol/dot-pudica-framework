namespace DotPudica.Core.Binding;

/// <summary>
/// Final Min/Max/Value for Godot <c>Range</c> writes, independent of call order (always Min → Max → Value).
/// </summary>
public readonly struct RangeApplyResult
{
    public RangeApplyResult(double min, double max, double value)
    {
        Min = min;
        Max = max;
        Value = value;
    }

    public double Min { get; }
    public double Max { get; }
    public double Value { get; }
}

public static class RangeWriteResolver
{
    /// <summary>
    /// Unwritten slots keep the control's current value.
    /// When <paramref name="hasValue"/> is false, returned <see cref="RangeApplyResult.Value"/> is still clamped to the new range;
    /// the caller decides whether to write it back when only bounds changed.
    /// </summary>
    public static RangeApplyResult Resolve(
        double currentMin,
        double currentMax,
        double currentValue,
        bool hasMin,
        double desiredMin,
        bool hasMax,
        double desiredMax,
        bool hasValue,
        double desiredValue)
    {
        var min = hasMin ? desiredMin : currentMin;
        var max = hasMax ? desiredMax : currentMax;
        if (max < min)
            max = min;

        var rawValue = hasValue ? desiredValue : currentValue;
        var value = rawValue < min ? min : rawValue > max ? max : rawValue;
        return new RangeApplyResult(min, max, value);
    }
}
