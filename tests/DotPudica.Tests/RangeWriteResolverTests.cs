using DotPudica.Core.Binding;

namespace DotPudica.Tests;

public sealed class RangeWriteResolverTests
{
    [Fact]
    public void ValueBeforeMax_RetainsDesiredValueAfterMaxRaised()
    {
        // Simulate: write Value=130 first (when Max=100), then write Max=130.
        var afterValue = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: 100, currentValue: 0,
            hasMin: false, desiredMin: 0,
            hasMax: false, desiredMax: 0,
            hasValue: true, desiredValue: 130);

        Assert.Equal(100, afterValue.Value); // Clamped by current Max

        var afterMax = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: 100, currentValue: afterValue.Value,
            hasMin: false, desiredMin: 0,
            hasMax: true, desiredMax: 130,
            hasValue: true, desiredValue: 130);

        Assert.Equal(130, afterMax.Max);
        Assert.Equal(130, afterMax.Value);
    }

    [Fact]
    public void MaxBeforeValue_AppliesFullValue()
    {
        var afterMax = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: 100, currentValue: 0,
            hasMin: false, desiredMin: 0,
            hasMax: true, desiredMax: 130,
            hasValue: false, desiredValue: 0);

        Assert.Equal(130, afterMax.Max);

        var afterValue = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: afterMax.Max, currentValue: afterMax.Value,
            hasMin: false, desiredMin: 0,
            hasMax: true, desiredMax: 130,
            hasValue: true, desiredValue: 130);

        Assert.Equal(130, afterValue.Value);
        Assert.Equal(130, afterValue.Max);
    }

    [Fact]
    public void ValueOnly_ClampsToExistingMax()
    {
        var result = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: 100, currentValue: 0,
            hasMin: false, desiredMin: 0,
            hasMax: false, desiredMax: 0,
            hasValue: true, desiredValue: 150);

        Assert.Equal(100, result.Max);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public void MaxLoweredThenRaised_RestoresDesiredValue()
    {
        var lowered = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: 100, currentValue: 80,
            hasMin: false, desiredMin: 0,
            hasMax: true, desiredMax: 50,
            hasValue: true, desiredValue: 80);

        Assert.Equal(50, lowered.Max);
        Assert.Equal(50, lowered.Value);

        var raised = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: lowered.Max, currentValue: lowered.Value,
            hasMin: false, desiredMin: 0,
            hasMax: true, desiredMax: 100,
            hasValue: true, desiredValue: 80);

        Assert.Equal(100, raised.Max);
        Assert.Equal(80, raised.Value);
    }

    [Fact]
    public void MinRaisedAboveValue_ClampsValueUp()
    {
        var result = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: 100, currentValue: 10,
            hasMin: true, desiredMin: 40,
            hasMax: false, desiredMax: 0,
            hasValue: true, desiredValue: 10);

        Assert.Equal(40, result.Min);
        Assert.Equal(40, result.Value);
    }

    [Fact]
    public void DesiredMaxBelowMin_ForcesMaxToMin()
    {
        var result = RangeWriteResolver.Resolve(
            currentMin: 0, currentMax: 100, currentValue: 50,
            hasMin: true, desiredMin: 80,
            hasMax: true, desiredMax: 60,
            hasValue: true, desiredValue: 50);

        Assert.Equal(80, result.Min);
        Assert.Equal(80, result.Max);
        Assert.Equal(80, result.Value);
    }
}
