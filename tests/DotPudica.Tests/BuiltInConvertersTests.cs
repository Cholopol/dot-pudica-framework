using System.Globalization;
using DotPudica.Core.Binding.Converters;
using DotPudica.Tests.Fixtures;

namespace DotPudica.Tests;

/// <summary>
/// Built-in value converter unit tests. Covers BuiltInConverters Convert/ConvertBack behavior.
/// </summary>
public class BuiltInConvertersTests
{
    // ------ BoolNegateConverter ------

    [Fact]
    public void BoolNegate_Convert_TrueReturnsFalse()
    {
        var conv = BoolNegateConverter.Instance;
        Assert.False((bool)conv.Convert(true, typeof(bool))!);
    }

    [Fact]
    public void BoolNegate_Convert_FalseReturnsTrue()
    {
        var conv = BoolNegateConverter.Instance;
        Assert.True((bool)conv.Convert(false, typeof(bool))!);
    }

    [Fact]
    public void BoolNegate_Convert_NonBoolReturnsOriginal()
    {
        var conv = BoolNegateConverter.Instance;
        var input = "not a bool";
        Assert.Same(input, conv.Convert(input, typeof(object)));
    }

    [Fact]
    public void BoolNegate_ConvertBack_SameAsConvert()
    {
        var conv = BoolNegateConverter.Instance;
        Assert.True((bool)conv.ConvertBack(false, typeof(bool))!);
        Assert.False((bool)conv.ConvertBack(true, typeof(bool))!);
    }

    // ------ BoolToVisibilityConverter ------

    [Fact]
    public void BoolToVisibility_Convert_TrueReturnsTrue()
    {
        var conv = BoolToVisibilityConverter.Instance;
        Assert.True((bool)conv.Convert(true, typeof(bool))!);
    }

    [Fact]
    public void BoolToVisibility_Convert_FalseReturnsFalse()
    {
        var conv = BoolToVisibilityConverter.Instance;
        Assert.False((bool)conv.Convert(false, typeof(bool))!);
    }

    [Fact]
    public void BoolToVisibility_Convert_NonBoolReturnsFalse()
    {
        var conv = BoolToVisibilityConverter.Instance;
        Assert.False((bool)conv.Convert("string", typeof(bool))!);
    }

    [Fact]
    public void BoolToVisibility_ConvertBack_TrueReturnsTrue()
    {
        var conv = BoolToVisibilityConverter.Instance;
        Assert.True((bool)conv.ConvertBack(true, typeof(bool))!);
    }

    [Fact]
    public void BoolToVisibility_ConvertBack_FalseReturnsFalse()
    {
        var conv = BoolToVisibilityConverter.Instance;
        Assert.False((bool)conv.ConvertBack(false, typeof(bool))!);
    }

    // ------ IntToStringConverter ------

    [Fact]
    public void IntToString_Convert_ReturnsString()
    {
        var conv = IntToStringConverter.Instance;
        Assert.Equal("42", conv.Convert(42, typeof(string)));
    }

    [Fact]
    public void IntToString_Convert_NullReturnsEmpty()
    {
        var conv = IntToStringConverter.Instance;
        Assert.Equal("", conv.Convert(null, typeof(string)));
    }

    [Fact]
    public void IntToString_ConvertBack_ValidString_ReturnsInt()
    {
        var conv = IntToStringConverter.Instance;
        Assert.Equal(123, conv.ConvertBack("123", typeof(int)));
    }

    [Fact]
    public void IntToString_ConvertBack_InvalidString_ReturnsZero()
    {
        var conv = IntToStringConverter.Instance;
        Assert.Equal(0, conv.ConvertBack("abc", typeof(int)));
    }

    [Fact]
    public void IntToString_ConvertBack_NullString_ReturnsZero()
    {
        var conv = IntToStringConverter.Instance;
        Assert.Equal(0, conv.ConvertBack(null, typeof(int)));
    }

    // ------ FloatToStringConverter ------

    [Fact]
    public void FloatToString_Convert_DefaultFormat_IsF2()
    {
        var conv = FloatToStringConverter.Instance;

        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal("3.14", conv.Convert(3.14159f, typeof(string)));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void FloatToString_Convert_NullReturnsEmpty()
    {
        var conv = FloatToStringConverter.Instance;
        Assert.Equal("", conv.Convert(null, typeof(string)));
    }

    [Fact]
    public void FloatToString_ConvertBack_ValidString_ReturnsFloat()
    {
        var conv = FloatToStringConverter.Instance;

        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal(1.5f, conv.ConvertBack("1.5", typeof(float)));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void FloatToString_ConvertBack_InvalidString_ReturnsZero()
    {
        var conv = FloatToStringConverter.Instance;
        Assert.Equal(0f, conv.ConvertBack("not a number", typeof(float)));
    }

    // ------ ObjectToStringConverter ------

    [Fact]
    public void ObjectToString_Convert_CallsToString()
    {
        var conv = ObjectToStringConverter.Instance;
        var obj = new SimpleViewModel { Name = "Test" };
        Assert.Equal(obj.ToString(), conv.Convert(obj, typeof(string)));
    }

    [Fact]
    public void ObjectToString_Convert_NullReturnsEmpty()
    {
        var conv = ObjectToStringConverter.Instance;
        Assert.Equal("", conv.Convert(null, typeof(string)));
    }

    [Fact]
    public void ObjectToString_ConvertBack_ReturnsOriginal()
    {
        var conv = ObjectToStringConverter.Instance;
        var input = "some string";
        Assert.Same(input, conv.ConvertBack(input, typeof(string)));
    }

    // ------ StringToBoolConverter ------

    [Fact]
    public void StringToBool_Convert_NonEmptyString_ReturnsTrue()
    {
        var conv = StringToBoolConverter.Instance;
        Assert.True((bool)conv.Convert("hello", typeof(bool))!);
    }

    [Fact]
    public void StringToBool_Convert_EmptyString_ReturnsFalse()
    {
        var conv = StringToBoolConverter.Instance;
        Assert.False((bool)conv.Convert("", typeof(bool))!);
    }

    [Fact]
    public void StringToBool_Convert_WhitespaceString_ReturnsFalse()
    {
        var conv = StringToBoolConverter.Instance;
        Assert.False((bool)conv.Convert("   ", typeof(bool))!);
    }

    [Fact]
    public void StringToBool_Convert_Null_ReturnsFalse()
    {
        var conv = StringToBoolConverter.Instance;
        Assert.False((bool)conv.Convert(null, typeof(bool))!);
    }

    [Fact]
    public void StringToBool_Convert_NonString_ReturnsFalse()
    {
        var conv = StringToBoolConverter.Instance;
        Assert.False((bool)conv.Convert(42, typeof(bool))!);
    }

    [Fact]
    public void StringToBool_ConvertBack_ReturnsStringRepresentation()
    {
        var conv = StringToBoolConverter.Instance;
        Assert.Equal("True", conv.ConvertBack(true, typeof(string)));
        Assert.Equal("", conv.ConvertBack(null, typeof(string)));
    }
}
