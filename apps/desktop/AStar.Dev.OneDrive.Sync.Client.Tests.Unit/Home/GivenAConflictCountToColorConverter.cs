using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAConflictCountToColorConverter
{
    [Fact]
    public void when_count_is_0_then_dark_color_is_returned()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(0, typeof(Color), null, CultureInfo.CurrentCulture);

        _ = result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ff1a1917");
    }

    [Fact]
    public void when_count_is_1_then_red_color_is_returned()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(1, typeof(Color), null, CultureInfo.CurrentCulture);

        _ = result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void when_count_is_5_then_red_color_is_returned()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(5, typeof(Color), null, CultureInfo.CurrentCulture);

        _ = result.ShouldBeOfType<Color>();
        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void when_count_is_negative_then_dark_color_is_returned()
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
    public void when_count_is_zero_or_negative_then_dark_color_is_returned(int count)
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
    public void when_count_is_positive_then_red_color_is_returned(int count)
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(count, typeof(Color), null, CultureInfo.CurrentCulture);

        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ffe24b4a");
    }

    [Fact]
    public void when_value_is_null_then_dark_color_is_returned()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert(null, typeof(Color), null, CultureInfo.CurrentCulture);

        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ff1a1917");
    }

    [Fact]
    public void when_value_is_non_int_string_then_dark_color_is_returned()
    {
        var converter = new ConflictCountToColorConverter();

        object result = converter.Convert("not a number", typeof(Color), null, CultureInfo.CurrentCulture);

        var color = (Color)result;
        color.ToString().ToLower(CultureInfo.CurrentCulture).ShouldBe("#ff1a1917");
    }

    [Fact]
    public void when_instance_is_accessed_multiple_times_then_same_instance_is_returned()
    {
        var instance1 = ConflictCountToColorConverter.Instance;
        var instance2 = ConflictCountToColorConverter.Instance;

        instance1.ShouldBeSameAs(instance2);
    }

    [Fact]
    public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
    {
        var converter = new ConflictCountToColorConverter();

        _ = Should.Throw<NotSupportedException>(() =>
            converter.ConvertBack(Color.Parse("#FFE24B4A"), typeof(int), null, CultureInfo.CurrentCulture));
    }
}
