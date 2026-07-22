namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenOptionExtensions
{
    [Fact]
    public void when_try_get_value_is_called_on_some_then_returns_true_and_outputs_the_value()
    {
        var option = Option.Some(42);

        bool result = option.TryGetValue(out int value);

        result.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void when_try_get_value_is_called_on_none_then_returns_false_and_outputs_the_default()
    {
        var option = Option.None<int>();

        bool result = option.TryGetValue(out int value);

        result.ShouldBeFalse();
        value.ShouldBe(0);
    }

    [Fact]
    public void when_to_option_is_called_with_a_non_default_value_then_returns_some() =>
        42.ToOption().ShouldBeOfType<Option<int>.Some>();

    [Fact]
    public void when_to_option_is_called_with_a_default_value_type_value_then_returns_none() =>
        0.ToOption().ShouldBeOfType<Option<int>.None>();

    [Fact]
    public void when_to_option_is_called_with_a_null_reference_type_value_then_returns_none()
    {
        string? value = null;

        value.ToOption().ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void when_to_option_with_a_predicate_is_called_and_the_predicate_is_satisfied_then_returns_some() =>
        42.ToOption(value => value > 0).ShouldBeOfType<Option<int>.Some>();

    [Fact]
    public void when_to_option_with_a_predicate_is_called_and_the_predicate_is_not_satisfied_then_returns_none() =>
        (-1).ToOption(value => value > 0).ShouldBeOfType<Option<int>.None>();

    [Fact]
    public void when_to_option_is_called_with_a_nullable_struct_with_a_value_then_returns_some()
    {
        int? value = 42;

        value.ToOption().ShouldBeOfType<Option<int>.Some>();
    }

    [Fact]
    public void when_to_option_is_called_with_a_nullable_struct_without_a_value_then_returns_none()
    {
        int? value = null;

        value.ToOption().ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void when_map_is_called_on_some_then_transforms_the_value() =>
        Option.Some(42).Map(value => value * 2).ShouldBe(Option.Some(84));

    [Fact]
    public void when_map_is_called_on_none_then_returns_none() =>
        Option.None<int>().Map(value => value * 2).ShouldBe(Option.None<int>());

    [Fact]
    public void when_bind_is_called_on_some_then_chains_the_bound_option() =>
        Option.Some(42).Bind(value => Option.Some(value * 2)).ShouldBe(Option.Some(84));

    [Fact]
    public void when_bind_is_called_on_none_then_returns_none() =>
        Option.None<int>().Bind(value => Option.Some(value * 2)).ShouldBe(Option.None<int>());

    [Fact]
    public void when_to_nullable_is_called_on_some_then_returns_the_wrapped_value() =>
        Option.Some(42).ToNullable().ShouldBe(42);

    [Fact]
    public void when_to_nullable_is_called_on_none_then_returns_null() =>
        Option.None<int>().ToNullable().ShouldBeNull();

    [Fact]
    public void when_to_enumerable_is_called_on_some_then_returns_a_single_element_sequence() =>
        Option.Some(42).ToEnumerable().ShouldBe([42]);

    [Fact]
    public void when_to_enumerable_is_called_on_none_then_returns_an_empty_sequence() =>
        Option.None<int>().ToEnumerable().ShouldBeEmpty();

    [Fact]
    public void when_deconstruct_is_called_on_some_then_outputs_true_and_the_value()
    {
        var (isSome, value) = Option.Some(42);

        isSome.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void when_deconstruct_is_called_on_none_then_outputs_false_and_the_default()
    {
        var (isSome, value) = Option.None<int>();

        isSome.ShouldBeFalse();
        value.ShouldBe(0);
    }

    [Fact]
    public void when_tap_is_called_on_some_then_invokes_the_action_and_returns_the_original_option()
    {
        int tappedValue = 0;

        var option = Option.Some(42).Tap(value => tappedValue = value);

        tappedValue.ShouldBe(42);
        option.ShouldBe(Option.Some(42));
    }

    [Fact]
    public void when_tap_is_called_on_none_then_does_not_invoke_the_action_and_returns_the_original_option()
    {
        bool actionInvoked = false;

        var option = Option.None<int>().Tap(_ => actionInvoked = true);

        actionInvoked.ShouldBeFalse();
        option.ShouldBe(Option.None<int>());
    }

    [Fact]
    public void when_filter_is_called_on_some_and_the_predicate_is_satisfied_then_returns_the_original_option() =>
        Option.Some(42).Filter(value => value > 0).ShouldBe(Option.Some(42));

    [Fact]
    public void when_filter_is_called_on_some_and_the_predicate_is_not_satisfied_then_returns_none() =>
        Option.Some(42).Filter(value => value < 0).ShouldBe(Option.None<int>());

    [Fact]
    public void when_filter_is_called_on_none_then_returns_none() =>
        Option.None<int>().Filter(value => value > 0).ShouldBe(Option.None<int>());

    [Fact]
    public void when_map_or_default_is_called_on_some_then_returns_the_mapped_value() =>
        Option.Some(42).MapOrDefault(value => value * 2, -1).ShouldBe(84);

    [Fact]
    public void when_map_or_default_is_called_on_none_then_returns_the_default_value() =>
        Option.None<int>().MapOrDefault(value => value * 2, -1).ShouldBe(-1);

    [Fact]
    public void when_map_or_else_is_called_on_some_then_returns_the_mapped_value() =>
        Option.Some(42).MapOrElse(value => value * 2, () => -1).ShouldBe(84);

    [Fact]
    public void when_map_or_else_is_called_on_none_then_returns_the_computed_default_value() =>
        Option.None<int>().MapOrElse(value => value * 2, () => -1).ShouldBe(-1);

    [Fact]
    public void when_values_is_called_on_a_sequence_of_options_then_returns_only_the_unwrapped_some_values()
    {
        List<Option<int>> options = [Option.Some(1), Option.None<int>(), Option.Some(3)];

        options.Values().ShouldBe([1, 3]);
    }

    [Fact]
    public void when_values_is_called_on_a_sequence_with_no_some_options_then_returns_an_empty_sequence()
    {
        List<Option<int>> options = [Option.None<int>(), Option.None<int>()];

        options.Values().ShouldBeEmpty();
    }

    [Fact]
    public void when_choose_with_a_predicate_is_called_then_returns_options_only_for_matching_elements()
    {
        List<int> source = [1, 2, 3, 4];

        var actual = source.Choose(value => value % 2 == 0).ToList();

        actual.Count.ShouldBe(2);
        actual[0].ShouldBe(Option.Some(2));
        actual[1].ShouldBe(Option.Some(4));
    }

    [Fact]
    public void when_choose_with_a_chooser_function_is_called_then_returns_only_the_some_results()
    {
        List<int> source = [1, 2, 3, 4];

        var actual = source.Choose(value => value % 2 == 0 ? Option.Some(value * 10) : Option.None<int>());

        actual.ShouldBe([20, 40]);
    }
}
