using System.Runtime.CompilerServices;
using DotPudica.Core.Binding;

namespace DotPudica.Godot.Binding;

/// <summary>
/// Desired value slots declaratively bindable on the Godot <see cref="global::Godot.Range"/> control.
/// </summary>
public enum RangeBindingProperty
{
    Value,
    MinValue,
    MaxValue
}

/// <summary>
/// Coordinated write of desired values for Godot <see cref="global::Godot.Range"/>: Min/Max/Value bindings on the same control instance
/// are committed in Min → Max → Value order regardless of registration or notification order; out-of-range desired values are clamped
/// to the current Min/Max window before being written back.
/// </summary>
public static class GodotRangeBinding
{
    private static readonly ConditionalWeakTable<global::Godot.Range, DesiredState> States = new();

    /// <summary>
    /// Recognizes <c>Value</c> / <c>MinValue</c> / <c>MaxValue</c> (case-insensitive); other names return false.
    /// </summary>
    public static bool TryParseProperty(string? targetProperty, out RangeBindingProperty property)
    {
        if (targetProperty?.Equals("MinValue", StringComparison.OrdinalIgnoreCase) == true)
        {
            property = RangeBindingProperty.MinValue;
            return true;
        }

        if (targetProperty?.Equals("MaxValue", StringComparison.OrdinalIgnoreCase) == true)
        {
            property = RangeBindingProperty.MaxValue;
            return true;
        }

        if (targetProperty?.Equals("Value", StringComparison.OrdinalIgnoreCase) == true)
        {
            property = RangeBindingProperty.Value;
            return true;
        }

        property = default;
        return false;
    }

    public static void SetProperty(global::Godot.Range range, RangeBindingProperty property, double value)
    {
        ArgumentNullException.ThrowIfNull(range);
        var state = States.GetOrCreateValue(range);
        switch (property)
        {
            case RangeBindingProperty.MinValue:
                state.HasMin = true;
                state.DesiredMin = value;
                break;
            case RangeBindingProperty.MaxValue:
                state.HasMax = true;
                state.DesiredMax = value;
                break;
            default:
                state.HasValue = true;
                state.DesiredValue = value;
                break;
        }

        Apply(range, state);
    }

    /// <summary>Reads the current displayed value of the control (for two-way binding, reads the real-time value from the user/engine side, not the uncommitted desired cache).</summary>
    public static double GetProperty(global::Godot.Range range, RangeBindingProperty property)
    {
        ArgumentNullException.ThrowIfNull(range);
        return property switch
        {
            RangeBindingProperty.MinValue => range.MinValue,
            RangeBindingProperty.MaxValue => range.MaxValue,
            _ => range.Value
        };
    }

    /// <summary>
    /// Writes back to the control by bound slots: Min → Max → Value.
    /// Only writes slots that were previously written via <see cref="SetProperty"/>; does not write Value if it was not bound
    /// (even if Resolve computes a clamped current value due to boundary changes).
    /// </summary>
    private static void Apply(global::Godot.Range range, DesiredState state)
    {
        var result = RangeWriteResolver.Resolve(
            range.MinValue,
            range.MaxValue,
            range.Value,
            state.HasMin,
            state.DesiredMin,
            state.HasMax,
            state.DesiredMax,
            state.HasValue,
            state.DesiredValue);

        if (state.HasMin)
            range.MinValue = result.Min;
        if (state.HasMax)
            range.MaxValue = result.Max;
        if (state.HasValue)
            range.Value = result.Value;
    }

    private sealed class DesiredState
    {
        public bool HasMin;
        public bool HasMax;
        public bool HasValue;
        public double DesiredMin;
        public double DesiredMax;
        public double DesiredValue;
    }
}