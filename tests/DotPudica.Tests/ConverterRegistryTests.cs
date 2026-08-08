using DotPudica.Core.Binding;
using DotPudica.Core.Binding.Converters;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// ConverterRegistry unit tests. Verifies converter registration and lookup mechanism.
/// </summary>
[Collection(FrameworkStaticCollection.Name)]
public class ConverterRegistryTests
{
    public ConverterRegistryTests()
    {
        ConverterRegistry.Clear();
    }

    [Fact]
    public void TryGet_Unregistered_ReturnsFalse()
    {
        Assert.False(ConverterRegistry.TryGet(typeof(BoolNegateConverter), out var converter));
        Assert.Null(converter);
    }

    [Fact]
    public void TryGet_NullType_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ConverterRegistry.TryGet(null!, out _));
    }

    [Fact]
    public void Register_ThenTryGet_ReturnsSameInstance()
    {
        var custom = new ReverseStringConverter();
        ConverterRegistry.Register(custom);

        Assert.True(ConverterRegistry.TryGet(typeof(ReverseStringConverter), out var retrieved));
        Assert.Same(custom, retrieved);
    }

    [Fact]
    public void Register_ThenTryGetTyped_ReturnsTypedInstance()
    {
        var custom = new ReverseStringConverter();
        ConverterRegistry.Register(custom);

        Assert.True(ConverterRegistry.TryGetTyped<string, string>(typeof(ReverseStringConverter), out var typed));
        Assert.Same(custom, typed);
    }

    [Fact]
    public void TryGetTyped_WrongTypeArgs_ReturnsFalse()
    {
        ConverterRegistry.Register(new ReverseStringConverter());

        Assert.False(ConverterRegistry.TryGetTyped<int, string>(typeof(ReverseStringConverter), out var typed));
        Assert.Null(typed);
    }

    [Fact]
    public void Clear_RemovesRegisteredInstances()
    {
        ConverterRegistry.Register(new ReverseStringConverter());
        ConverterRegistry.Clear();

        Assert.False(ConverterRegistry.TryGet(typeof(ReverseStringConverter), out _));
    }
}
