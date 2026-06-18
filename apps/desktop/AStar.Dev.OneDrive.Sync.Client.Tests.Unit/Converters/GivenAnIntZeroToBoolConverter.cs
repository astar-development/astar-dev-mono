using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public sealed class GivenAnIntZeroToBoolConverter
{
    private static readonly IntZeroToBoolConverter Sut = IntZeroToBoolConverter.Instance;

    [Fact]
    public void when_value_is_zero_then_result_is_true()
    {
        object result = Sut.Convert(0, typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(true);
    }

    [Fact]
    public void when_value_is_one_then_result_is_false()
    {
        object result = Sut.Convert(1, typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void when_value_is_negative_then_result_is_false()
    {
        object result = Sut.Convert(-1, typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void when_value_is_null_then_result_is_false()
    {
        object result = Sut.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void when_value_is_non_integer_then_result_is_false()
    {
        object result = Sut.Convert("zero", typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void when_instance_accessed_multiple_times_then_same_instance_is_returned()
    {
        var first = IntZeroToBoolConverter.Instance;
        var second = IntZeroToBoolConverter.Instance;

        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void when_convert_back_is_called_then_not_supported_exception_is_thrown()
    {
        _ = Should.Throw<NotSupportedException>(() =>
            Sut.ConvertBack(true, typeof(int), null, CultureInfo.InvariantCulture));
    }
}
