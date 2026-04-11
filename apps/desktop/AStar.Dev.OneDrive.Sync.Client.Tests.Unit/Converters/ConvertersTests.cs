using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public sealed class DepthToIndentConverterTests
{
    [Fact]
    public void Convert_WithDepth0_ShouldReturn0()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(0, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void Convert_WithDepth1_ShouldReturn16()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(1, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(16.0);
    }

    [Fact]
    public void Convert_WithDepth2_ShouldReturn32()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(2, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(32.0);
    }

    [Fact]
    public void Convert_WithDepth5_ShouldReturn80()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(5, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(80.0);
    }

    [Fact]
    public void Convert_WithDepth10_ShouldReturn160()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(10, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(160.0);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, 16.0)]
    [InlineData(2, 32.0)]
    [InlineData(3, 48.0)]
    [InlineData(4, 64.0)]
    [InlineData(5, 80.0)]
    [InlineData(10, 160.0)]
    [InlineData(100, 1600.0)]
    public void Convert_WithVaryingDepths_ShouldMultiplyBy16(int depth, double expected)
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(depth, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Convert_WithNullValue_ShouldReturn0()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(null, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void Convert_WithStringValue_ShouldReturn0()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert("not an int", typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void Convert_WithNegativeDepth_ShouldReturnNegativeValue()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(-5, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(-80.0);
    }

    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        var instance1 = DepthToIndentConverter.Instance;
        var instance2 = DepthToIndentConverter.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void ConvertBack_ShouldThrowNotSupported()
    {
        var converter = new DepthToIndentConverter();

        _ = Should.Throw<NotSupportedException>(() =>
            converter.ConvertBack(16.0, typeof(int), null, CultureInfo.CurrentCulture));
    }
}

public class ConflictCountToColorConverterTests
{
    [Fact]
    public void Convert_WithCount0_ShouldReturnDarkColor()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(0, typeof(Color), null, CultureInfo.CurrentCulture);

        _ = result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ff1a1917");
    }

    [Fact]
    public void Convert_WithCount1_ShouldReturnRedColor()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(1, typeof(Color), null, CultureInfo.CurrentCulture);

        _ = result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void Convert_WithCountGreaterThan0_ShouldReturnRedColor()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(5, typeof(Color), null, CultureInfo.CurrentCulture);

        _ = result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void Convert_WithNegativeCount_ShouldReturnDarkColor()
    {
        var converter = new ConflictCountToColorConverter();
        object result = converter.Convert(-1, typeof(Color), null, CultureInfo.CurrentCulture);

        _ = result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ff1a1917");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Convert_WithZeroOrNegative_ShouldReturnDark(int count)
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(count, typeof(Color), null, CultureInfo.CurrentCulture);

        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ff1a1917");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Convert_WithAnyPositiveCount_ShouldReturnRed(int count)
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(count, typeof(Color), null, CultureInfo.CurrentCulture);

        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void Convert_WithNullValue_ShouldReturnDarkColor()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(null, typeof(Color), null, CultureInfo.CurrentCulture);

        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ff1a1917");
    }

    [Fact]
    public void Convert_WithStringValue_ShouldReturnDarkColor()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert("not a number", typeof(Color), null, CultureInfo.CurrentCulture);

        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ff1a1917");
    }

    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        var instance1 = ConflictCountToColorConverter.Instance;
        var instance2 = ConflictCountToColorConverter.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void ConvertBack_ShouldThrowNotSupported()
    {
        var converter = new ConflictCountToColorConverter();

        _ = Should.Throw<NotSupportedException>(() =>
            converter.ConvertBack(Color.Parse("#FFE24B4A"), typeof(int), null, CultureInfo.CurrentCulture));
    }
}
