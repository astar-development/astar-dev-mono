using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Home;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public sealed class GivenADepthToIndentConverter
{
    [Fact]
    public void when_depth_is_0_then_result_is_0()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(0, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void when_depth_is_1_then_result_is_16()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(1, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(16.0);
    }

    [Fact]
    public void when_depth_is_2_then_result_is_32()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(2, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(32.0);
    }

    [Fact]
    public void when_depth_is_5_then_result_is_80()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(5, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(80.0);
    }

    [Fact]
    public void when_depth_is_10_then_result_is_160()
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
    public void when_depth_varies_then_result_is_depth_times_16(int depth, double expected)
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(depth, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void when_value_is_null_then_result_is_0()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(null, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void when_value_is_non_int_string_then_result_is_0()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert("not an int", typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(0.0);
    }

    [Fact]
    public void when_depth_is_negative_then_result_is_negative()
    {
        var converter = new DepthToIndentConverter();

        object result = converter.Convert(-5, typeof(double), null, CultureInfo.CurrentCulture);

        result.ShouldBe(-80.0);
    }

    [Fact]
    public void when_instance_is_accessed_multiple_times_then_same_instance_is_returned()
    {
        var instance1 = DepthToIndentConverter.Instance;
        var instance2 = DepthToIndentConverter.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
    {
        var converter = new DepthToIndentConverter();

        _ = Should.Throw<NotSupportedException>(() =>
            converter.ConvertBack(16.0, typeof(int), null, CultureInfo.CurrentCulture));
    }
}

