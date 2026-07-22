namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenOption
{
    [Fact]
    public void when_some_is_called_with_a_value_then_returns_a_some_instance()
    {
        var option = Option.Some(42);

        option.ShouldBeOfType<Option<int>.Some>();
    }

    [Fact]
    public void when_none_is_called_then_returns_the_shared_none_instance()
    {
        var option = Option.None<int>();

        option.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void when_none_is_called_multiple_times_then_returns_the_same_instance() =>
        ReferenceEquals(Option.None<int>(), Option.None<int>()).ShouldBeTrue();

    [Fact]
    public void when_some_constructor_is_called_with_a_null_value_then_throws_argument_null_exception()
    {
        Action someAction = () => _ = new Option<string>.Some(null!);

        _ = someAction.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void when_a_non_null_value_is_implicitly_converted_then_becomes_some()
    {
        Option<int> option = 42;

        option.ShouldBeOfType<Option<int>.Some>();
    }

    [Fact]
    public void when_a_null_reference_value_is_implicitly_converted_then_becomes_none()
    {
        string? value = null;

        Option<string> option = value!;

        option.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void when_match_is_called_on_some_then_invokes_on_some()
    {
        var option = Option.Some(42);

        int actual = option.Match(value => value * 2, () => -1);

        actual.ShouldBe(84);
    }

    [Fact]
    public void when_match_is_called_on_none_then_invokes_on_none()
    {
        var option = Option.None<int>();

        int actual = option.Match(value => value * 2, () => -1);

        actual.ShouldBe(-1);
    }

    [Fact]
    public void when_some_value_is_read_then_returns_the_wrapped_value() =>
        ((Option<int>.Some)Option.Some(42)).Value.ShouldBe(42);

    [Fact]
    public void when_some_to_string_is_called_then_returns_the_expected_format() =>
        Option.Some(42).ToString().ShouldBe("Some(42)");

    [Fact]
    public void when_none_to_string_is_called_then_returns_the_expected_format() =>
        Option.None<int>().ToString().ShouldBe("None");

    [Fact]
    public void when_two_some_options_with_equal_values_are_compared_with_equals_then_returns_true() =>
        Option.Some(42).Equals(Option.Some(42)).ShouldBeTrue();

    [Fact]
    public void when_two_some_options_with_different_values_are_compared_with_equals_then_returns_false() =>
        Option.Some(42).Equals(Option.Some(7)).ShouldBeFalse();

    [Fact]
    public void when_two_none_options_are_compared_with_equals_then_returns_true() =>
        Option.None<int>().Equals(Option.None<int>()).ShouldBeTrue();

    [Fact]
    public void when_a_some_option_is_compared_with_a_none_option_using_equals_then_returns_false() =>
        Option.Some(42).Equals(Option.None<int>()).ShouldBeFalse();

    [Fact]
    public void when_an_option_is_compared_with_a_non_option_object_using_equals_then_returns_false() =>
        Option.Some(42).Equals(new object()).ShouldBeFalse();

    [Fact]
    public void when_an_option_is_compared_with_null_using_equals_then_returns_false() =>
        Option.Some(42).Equals(null).ShouldBeFalse();

    [Fact]
    public void when_two_some_options_with_equal_values_are_compared_with_equality_operator_then_returns_true() =>
        (Option.Some(42) == Option.Some(42)).ShouldBeTrue();

    [Fact]
    public void when_two_some_options_with_different_values_are_compared_with_equality_operator_then_returns_false() =>
        (Option.Some(42) == Option.Some(7)).ShouldBeFalse();

    [Fact]
    public void when_two_some_options_with_equal_values_are_compared_with_inequality_operator_then_returns_false() =>
        (Option.Some(42) != Option.Some(42)).ShouldBeFalse();

    [Fact]
    public void when_two_some_options_with_different_values_are_compared_with_inequality_operator_then_returns_true() =>
        (Option.Some(42) != Option.Some(7)).ShouldBeTrue();

    [Fact]
    public void when_a_null_left_operand_is_compared_with_a_null_right_operand_using_equality_operator_then_returns_true()
    {
        Option<int>? left = null;
        Option<int>? right = null;

        (left! == right!).ShouldBeTrue();
    }

    [Fact]
    public void when_a_null_left_operand_is_compared_with_a_non_null_right_operand_using_equality_operator_then_returns_false()
    {
        Option<int>? left = null;

        (left! == Option.Some(42)).ShouldBeFalse();
    }

    [Fact]
    public void when_two_some_options_with_equal_values_are_compared_then_hash_codes_are_equal() =>
        Option.Some(42).GetHashCode().ShouldBe(Option.Some(42).GetHashCode());

    [Fact]
    public void when_two_none_options_are_compared_then_hash_codes_are_equal() =>
        Option.None<int>().GetHashCode().ShouldBe(Option.None<int>().GetHashCode());

    [Fact]
    public void when_a_some_option_and_a_none_option_are_compared_then_hash_codes_are_different() =>
        Option.Some(42).GetHashCode().ShouldNotBe(Option.None<int>().GetHashCode());
}
