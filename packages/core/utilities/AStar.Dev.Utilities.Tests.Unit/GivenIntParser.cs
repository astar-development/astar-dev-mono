namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class GivenIntParser
{
    [Fact]
    public void when_toint_is_called_with_a_valid_value_then_returns_the_expected_value() =>
        "123".ToInt().ShouldBe(123);

    [Fact]
    public void when_toint_is_called_with_a_negative_value_then_returns_the_expected_value() =>
        "-123".ToInt().ShouldBe(-123);

    [Fact]
    public void when_toint_is_called_with_an_invalid_value_then_throws_format_exception() =>
        Should.Throw<FormatException>(() => "Invalid".ToInt());

    [Fact]
    public void when_toint_is_called_with_an_empty_value_then_throws_format_exception() =>
        Should.Throw<FormatException>(() => "".ToInt());

    [Fact]
    public void when_toint_is_called_with_a_decimal_value_then_throws_format_exception() =>
        Should.Throw<FormatException>(() => "123.45".ToInt());

    [Fact]
    public void when_tointsafe_is_called_with_an_invalid_value_then_returns_zero() =>
        "Invalid".ToIntSafe().ShouldBe(0);

    [Fact]
    public void when_tointsafe_is_called_with_an_empty_value_then_returns_zero() =>
        "".ToIntSafe().ShouldBe(0);

    [Fact]
    public void when_tointsafe_is_called_with_a_valid_value_then_returns_the_expected_value() =>
        "12345".ToIntSafe().ShouldBe(12345);

    [Fact]
    public void when_tointsafe_is_called_with_a_negative_value_then_returns_the_expected_value() =>
        "-12345".ToIntSafe().ShouldBe(-12345);
}