using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Dashboard;

public sealed class GivenAConflictCountToColorConverter
{
    private static readonly ConflictCountToColorConverter Sut = ConflictCountToColorConverter.Instance;

    [Fact]
    public void when_conflict_count_is_zero_then_primary_text_color_is_returned()
    {
        var result = (Color)Sut.Convert(0, typeof(Color), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Color.Parse("#1A1917"));
    }

    [Fact]
    public void when_conflict_count_is_positive_then_red_color_is_returned()
    {
        var result = (Color)Sut.Convert(1, typeof(Color), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Color.Parse("#E24B4A"));
    }

    [Fact]
    public void when_conflict_count_is_greater_than_one_then_red_color_is_returned()
    {
        var result = (Color)Sut.Convert(5, typeof(Color), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Color.Parse("#E24B4A"));
    }

    [Fact]
    public void when_value_is_null_then_primary_text_color_is_returned()
    {
        var result = (Color)Sut.Convert(null, typeof(Color), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Color.Parse("#1A1917"));
    }

    [Fact]
    public void when_value_is_non_integer_then_primary_text_color_is_returned()
    {
        var result = (Color)Sut.Convert("not a number", typeof(Color), null, CultureInfo.InvariantCulture);

        result.ShouldBe(Color.Parse("#1A1917"));
    }

    [Fact]
    public void when_instance_accessed_multiple_times_then_same_instance_is_returned()
    {
        var first = ConflictCountToColorConverter.Instance;
        var second = ConflictCountToColorConverter.Instance;

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
    {
        _ = Should.Throw<NotSupportedException>(() =>
            Sut.ConvertBack(Colors.Transparent, typeof(int), null, CultureInfo.InvariantCulture));
    }
}
