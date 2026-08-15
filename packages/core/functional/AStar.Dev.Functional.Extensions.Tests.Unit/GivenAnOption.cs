namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public sealed class GivenAnOption
{
    private static readonly int[] ExpectedArrayOfInts = [1, 2, 3];
    private static readonly int[] ExpectedArrayOfInts2 = [20, 40];

    [Fact]
    public void when_option_is_some_then_match_invokes_the_some_handler()
    {
        var option = Option.Some(42);

        string matched = option.Match(
                                   some => $"Some: {some}",
                                   () => "None");

        matched.ShouldBe("Some: 42");
    }

    [Fact]
    public void when_option_is_none_then_match_invokes_the_none_handler()
    {
        var option = Option.None<int>();

        string matched = option.Match(
                                   some => $"Some: {some}",
                                   () => "None");

        matched.ShouldBe("None");
    }

    [Fact]
    public async Task when_option_is_some_then_match_async_invokes_the_async_some_handler()
    {
        var option = Option.Some(42);

        string matched = await option.MatchAsync(
                                              some => Task.FromResult($"Some: {some}"),
                                              () => "None");

        matched.ShouldBe("Some: 42");
    }

    [Fact]
    public async Task when_option_is_none_then_match_async_invokes_the_none_handler()
    {
        var option = Option.None<int>();

        string matched = await option.MatchAsync(
                                              some => Task.FromResult($"Some: {some}"),
                                              () => "None");

        matched.ShouldBe("None");
    }

    [Fact]
    public async Task when_option_is_some_then_match_async_invokes_the_sync_some_handler_with_async_none_handler()
    {
        var option = Option.Some(42);

        string matched = await option.MatchAsync(
                                              some => $"Some: {some}",
                                              () => Task.FromResult("None"));

        matched.ShouldBe("Some: 42");
    }

    [Fact]
    public async Task when_option_is_none_then_match_async_invokes_the_async_none_handler_with_sync_some_handler()
    {
        var option = Option.None<int>();

        string matched = await option.MatchAsync(
                                              some => $"Some: {some}",
                                              () => Task.FromResult("None"));

        matched.ShouldBe("None");
    }

    [Fact]
    public async Task when_option_is_some_then_match_async_invokes_the_async_some_handler_with_async_none_handler()
    {
        var option = Option.Some(42);

        string matched = await option.MatchAsync(
                                              some => Task.FromResult($"Some: {some}"),
                                              () => Task.FromResult("None"));

        matched.ShouldBe("Some: 42");
    }

    [Fact]
    public async Task when_option_is_none_then_match_async_invokes_the_async_none_handler_with_async_some_handler()
    {
        var option = Option.None<int>();

        string matched = await option.MatchAsync(
                                              some => Task.FromResult($"Some: {some}"),
                                              () => Task.FromResult("None"));

        matched.ShouldBe("None");
    }

    [Fact]
    public void when_some_is_created_then_it_holds_the_correct_value()
    {
        int value = 42;

        var option = Option.Some(value);

        ((Option<int>.Some)option).Value.ShouldBe(value);
    }

    [Fact]
    public void when_some_is_created_with_a_null_value_then_an_argument_null_exception_is_thrown() => Should.Throw<ArgumentNullException>(() => Option.Some<string>(null!));

    [Fact]
    public void when_a_value_is_implicitly_converted_then_it_becomes_some()
    {
        string value = "test";

        Option<string> option = value;

        _ = option.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)option).Value.ShouldBe("test");
    }

    [Fact]
    public void when_null_is_implicitly_converted_then_it_becomes_none()
    {
        string? value = null;

        Option<string> option = value!;

        _ = option.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void when_option_is_some_then_try_get_value_returns_true_with_the_value()
    {
        var option = Option.Some(42);

        bool success = option.TryGetValue(out int value);

        success.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void when_option_is_none_then_try_get_value_returns_false_with_the_default_value()
    {
        var option = Option.None<int>();

        bool success = option.TryGetValue(out int value);

        success.ShouldBeFalse();
        value.ShouldBe(default);
    }

    [Fact]
    public void when_to_option_is_called_on_a_value_then_it_becomes_some()
    {
        string value = "test";

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)option).Value.ShouldBe("test");
    }

    [Fact]
    public void when_to_option_is_called_on_null_then_it_becomes_none()
    {
        string? value = null;

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void when_to_option_is_called_with_a_true_predicate_then_it_becomes_some()
    {
        int value = 42;

        var option = value.ToOption(x => x > 0);

        _ = option.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)option).Value.ShouldBe(42);
    }

    [Fact]
    public void when_to_option_is_called_with_a_false_predicate_then_it_becomes_none()
    {
        int value = 42;

        var option = value.ToOption(x => x < 0);

        _ = option.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void when_a_nullable_with_a_value_is_converted_then_it_becomes_some()
    {
        int? value = 42;

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)option).Value.ShouldBe(42);
    }

    [Fact]
    public void when_a_nullable_without_a_value_is_converted_then_it_becomes_none()
    {
        int? value = null;

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void when_option_is_some_then_map_transforms_the_value()
    {
        var option = Option.Some(42);

        var result = option.Map(x => x.ToString());

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public void when_option_is_none_then_map_returns_none()
    {
        var option = Option.None<int>();

        var result = option.Map(x => x.ToString());

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void when_option_is_some_then_bind_returns_the_new_option()
    {
        var option = Option.Some(42);

        var result = option.Bind(x => Option.Some(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public void when_option_is_none_then_bind_returns_none()
    {
        var option = Option.None<int>();

        var result = option.Bind(x => Option.Some(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void when_bind_function_returns_none_then_the_result_is_none()
    {
        var option = Option.Some(42);

        var result = option.Bind(_ => Option.None<string>());

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void when_option_is_some_then_to_result_returns_ok()
    {
        var option = Option.Some(42);

        var result = option.ToResult(() => "Error");

        _ = result.ShouldBeOfType<Result<int, string>.Ok>();
        ((Result<int, string>.Ok)result).Value.ShouldBe(42);
    }

    [Fact]
    public void when_option_is_none_then_to_result_returns_error()
    {
        var option = Option.None<int>();

        var result = option.ToResult(() => "Error Message");

        _ = result.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)result).Reason.ShouldBe("Error Message");
    }

    [Fact]
    public void when_option_is_some_then_to_nullable_returns_the_value()
    {
        var option = Option.Some(42);

        int? result = option.ToNullable();

        _ = result.ShouldNotBeNull();
        result.ShouldBe(42);
    }

    [Fact]
    public void when_option_is_none_then_to_nullable_returns_null()
    {
        var option = Option.None<int>();

        int? result = option.ToNullable();

        result.ShouldBeNull();
    }

    [Fact]
    public void when_option_is_some_then_to_enumerable_returns_a_single_item_sequence()
    {
        var option = Option.Some(42);

        var result = option.ToEnumerable().ToList();

        result.Count.ShouldBe(1);
        result[0].ShouldBe(42);
    }

    [Fact]
    public void when_option_is_none_then_to_enumerable_returns_an_empty_sequence()
    {
        var option = Option.None<int>();

        var result = option.ToEnumerable().ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void when_option_is_some_then_deconstruction_returns_true_and_the_value()
    {
        var option = Option.Some(42);

        (bool isSome, int value) = option;

        isSome.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void when_option_is_none_then_deconstruction_returns_false_and_the_default_value()
    {
        var option = Option.None<int>();

        (bool isSome, int value) = option;

        isSome.ShouldBeFalse();
        value.ShouldBe(default);
    }

    [Fact]
    public void when_two_some_options_have_equal_values_then_their_hash_codes_match()
    {
        var option1 = Option.Some(42);
        var option2 = Option.Some(42);

        option1.GetHashCode().ShouldBe(option2.GetHashCode());
    }

    [Fact]
    public void when_two_some_options_have_different_values_then_their_hash_codes_differ()
    {
        var option1 = Option.Some(42);
        var option2 = Option.Some(43);

        option1.GetHashCode().ShouldNotBe(option2.GetHashCode());
    }

    [Fact]
    public void when_two_none_options_are_compared_then_their_hash_codes_match()
    {
        var option1 = Option.None<int>();
        var option2 = Option.None<int>();

        option1.GetHashCode().ShouldBe(option2.GetHashCode());
    }

    [Fact]
    public void when_two_some_options_have_the_same_value_then_they_are_equal()
    {
        var option1 = Option.Some(42);
        var option2 = Option.Some(42);

        option1.ShouldBe(option2);
        (option1 == option2).ShouldBeTrue();
        (option1 != option2).ShouldBeFalse();
    }

    [Fact]
    public void when_two_some_options_have_different_values_then_they_are_not_equal()
    {
        var option1 = Option.Some(42);
        var option2 = Option.Some(43);

        option1.ShouldNotBe(option2);
        (option1 == option2).ShouldBeFalse();
        (option1 != option2).ShouldBeTrue();
    }

    [Fact]
    public void when_two_none_options_are_compared_then_they_are_equal()
    {
        var option1 = Option.None<int>();
        var option2 = Option.None<int>();

        option1.ShouldBe(option2);
        (option1 == option2).ShouldBeTrue();
        (option1 != option2).ShouldBeFalse();
    }

    [Fact]
    public void when_a_some_option_is_compared_to_a_none_option_then_they_are_not_equal()
    {
        var option1 = Option.Some(42);
        var option2 = Option.None<int>();

        option1.ShouldNotBe(option2);
        (option1 == option2).ShouldBeFalse();
        (option1 != option2).ShouldBeTrue();
    }

    [Fact]
    public void when_option_is_some_then_to_string_returns_the_expected_representation()
    {
        var option = Option.Some(42);

        string? result = option.ToString();

        result.ShouldBe("Some(42)");
    }

    [Fact]
    public void when_option_is_none_then_to_string_returns_the_expected_representation()
    {
        var option = Option.None<int>();

        string? result = option.ToString();

        result.ShouldBe("None");
    }

    [Fact]
    public void when_option_is_some_then_tap_executes_the_side_effect()
    {
        var option = Option.Some(42);
        bool sideEffectExecuted = false;
        int capturedValue = 0;

        var result = option.Tap(x =>
                                {
                                    sideEffectExecuted = true;
                                    capturedValue = x;
                                });

        sideEffectExecuted.ShouldBeTrue();
        capturedValue.ShouldBe(42);
        result.ShouldBeSameAs(option);
    }

    [Fact]
    public void when_option_is_none_then_tap_does_not_execute_the_side_effect()
    {
        var option = Option.None<int>();
        bool sideEffectExecuted = false;

        var result = option.Tap(_ => sideEffectExecuted = true);

        sideEffectExecuted.ShouldBeFalse();
        result.ShouldBeSameAs(option);
    }

    [Fact]
    public void when_filter_predicate_is_true_then_the_same_option_is_returned()
    {
        var option = Option.Some(42);

        var result = option.Filter(x => x > 0);

        _ = result.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)result).Value.ShouldBe(42);
    }

    [Fact]
    public void when_filter_predicate_is_false_then_none_is_returned()
    {
        var option = Option.Some(42);

        var result = option.Filter(x => x < 0);

        _ = result.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void when_option_is_some_then_map_or_default_returns_the_transformed_value()
    {
        var option = Option.Some(42);

        int result = option.MapOrDefault(x => x * 2, -1);

        result.ShouldBe(84);
    }

    [Fact]
    public void when_option_is_none_then_map_or_default_returns_the_default_value()
    {
        var option = Option.None<int>();

        int result = option.MapOrDefault(x => x * 2, -1);

        result.ShouldBe(-1);
    }

    [Fact]
    public void when_option_is_some_then_map_or_else_returns_the_transformed_value_without_calling_the_factory()
    {
        var option = Option.Some(42);
        bool factoryCalled = false;

        int result = option.MapOrElse(
                                      x => x * 2,
                                      () =>
                                      {
                                          factoryCalled = true;

                                          return -1;
                                      }
                                     );

        result.ShouldBe(84);
        factoryCalled.ShouldBeFalse();
    }

    [Fact]
    public void when_option_is_none_then_map_or_else_uses_the_factory()
    {
        var option = Option.None<int>();
        bool factoryCalled = false;

        int result = option.MapOrElse(
                                      x => x * 2,
                                      () =>
                                      {
                                          factoryCalled = true;

                                          return -1;
                                      }
                                     );

        result.ShouldBe(-1);
        factoryCalled.ShouldBeTrue();
    }

    [Fact]
    public void when_values_is_called_on_a_collection_of_options_then_only_the_some_values_are_returned()
    {
        Option<int>[] options = [Option.Some(1), Option.None<int>(), Option.Some(2), Option.None<int>(), Option.Some(3)];

        var result = options.Values().ToList();

        result.Count.ShouldBe(3);
        result.ShouldBe(ExpectedArrayOfInts);
    }

    [Fact]
    public void when_choose_is_called_with_a_predicate_then_only_matching_items_are_returned_as_some()
    {
        int[] numbers = [1, 2, 3, 4, 5];

        var result = numbers.Choose(x => x % 2 == 0).ToList();

        result.Count.ShouldBe(2);
        result.All(option => option is Option<int>.Some).ShouldBeTrue();
        ((Option<int>.Some)result[0]).Value.ShouldBe(2);
        ((Option<int>.Some)result[1]).Value.ShouldBe(4);
    }

    [Fact]
    public void when_choose_is_called_with_a_chooser_function_then_matching_items_are_transformed()
    {
        int[] numbers = [1, 2, 3, 4, 5];

        var result = numbers.Choose(x => x % 2 == 0
                                             ? Option.Some(x * 10)
                                             : Option.None<int>()).ToList();

        result.Count.ShouldBe(2);
        result.ShouldBe(ExpectedArrayOfInts2);
    }

    [Fact]
    public async Task when_option_is_some_then_map_async_transforms_the_value()
    {
        var option = Option.Some(42);

        var result = await option.MapAsync(x => Task.FromResult(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task when_option_is_none_then_map_async_returns_none()
    {
        var option = Option.None<int>();

        var result = await option.MapAsync(x => Task.FromResult(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public async Task when_a_task_of_option_is_some_then_map_async_transforms_the_value()
    {
        var optionTask = Task.FromResult(Option.Some(42));

        var result = await optionTask.MapAsync(x => x.ToString());

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task when_a_task_of_option_is_some_then_map_async_with_an_async_mapper_transforms_the_value()
    {
        var optionTask = Task.FromResult(Option.Some(42));

        var result = await optionTask.MapAsync(x => Task.FromResult(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task when_option_is_some_then_bind_async_returns_the_new_option()
    {
        var option = Option.Some(42);

        var result = await option.BindAsync(x => Task.FromResult(Option.Some(x.ToString())));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task when_option_is_none_then_bind_async_returns_none()
    {
        var option = Option.None<int>();

        var result = await option.BindAsync(x => Task.FromResult(Option.Some(x.ToString())));

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public async Task when_option_is_some_then_to_result_async_returns_ok()
    {
        var option = Option.Some(42);

        var result = await option.ToResultAsync(() => Task.FromResult("Error"));

        _ = result.ShouldBeOfType<Result<int, string>.Ok>();
        ((Result<int, string>.Ok)result).Value.ShouldBe(42);
    }

    [Fact]
    public async Task when_option_is_none_then_to_result_async_returns_error()
    {
        var option = Option.None<int>();

        var result = await option.ToResultAsync(() => Task.FromResult("Error"));

        _ = result.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)result).Reason.ShouldBe("Error");
    }

    [Fact]
    public async Task when_option_is_some_then_tap_async_executes_the_side_effect()
    {
        var option = Option.Some(42);
        bool sideEffectExecuted = false;
        int capturedValue = 0;

        var result = await option.TapAsync(x =>
                                           {
                                               sideEffectExecuted = true;
                                               capturedValue = x;

                                               return Task.CompletedTask;
                                           });

        sideEffectExecuted.ShouldBeTrue();
        capturedValue.ShouldBe(42);
        result.ShouldBe(option);
    }

    [Fact]
    public async Task when_option_is_none_then_tap_async_does_not_execute_the_side_effect()
    {
        var option = Option.None<int>();
        bool sideEffectExecuted = false;

        var result = await option.TapAsync(_ =>
                                           {
                                               sideEffectExecuted = true;

                                               return Task.CompletedTask;
                                           });

        sideEffectExecuted.ShouldBeFalse();
        result.ShouldBe(option);
    }

    [Fact]
    public async Task when_a_task_of_option_is_some_then_bind_async_with_a_sync_binder_returns_the_new_option()
    {
        var optionTask = Task.FromResult(Option.Some(42));

        var result = await optionTask.BindAsync(x => Option.Some(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task when_a_task_of_option_is_none_then_to_result_async_with_a_sync_error_factory_returns_error()
    {
        var optionTask = Task.FromResult(Option.None<int>());

        var result = await optionTask.ToResultAsync(() => "Error");

        _ = result.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)result).Reason.ShouldBe("Error");
    }

    [Fact]
    public async Task when_a_task_of_option_is_none_then_to_result_async_with_an_async_error_factory_returns_error()
    {
        var optionTask = Task.FromResult(Option.None<int>());

        var result = await optionTask.ToResultAsync(() => Task.FromResult("Error"));

        _ = result.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)result).Reason.ShouldBe("Error");
    }

    [Fact]
    public async Task when_a_task_of_option_is_some_then_tap_async_with_a_sync_action_executes_the_side_effect()
    {
        var optionTask = Task.FromResult(Option.Some(42));
        bool sideEffectExecuted = false;
        int capturedValue = 0;

        var result = await optionTask.TapAsync(x =>
                                               {
                                                   sideEffectExecuted = true;
                                                   capturedValue = x;
                                               });

        sideEffectExecuted.ShouldBeTrue();
        capturedValue.ShouldBe(42);
        _ = result.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)result).Value.ShouldBe(42);
    }

    [Fact]
    public async Task when_a_task_of_option_is_some_then_tap_async_with_an_async_action_executes_the_side_effect()
    {
        var optionTask = Task.FromResult(Option.Some(42));
        bool sideEffectExecuted = false;
        int capturedValue = 0;

        var result = await optionTask.TapAsync(x =>
                                               {
                                                   sideEffectExecuted = true;
                                                   capturedValue = x;

                                                   return Task.CompletedTask;
                                               });

        sideEffectExecuted.ShouldBeTrue();
        capturedValue.ShouldBe(42);
        _ = result.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)result).Value.ShouldBe(42);
    }

    [Fact]
    public void when_to_option_is_called_on_a_non_default_value_then_it_becomes_some()
    {
        string value = "test";

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)option).Value.ShouldBe("test");
    }

    [Fact]
    public void when_to_option_is_called_on_a_default_value_then_it_becomes_none()
    {
        int value = default;

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void when_some_to_string_is_called_then_it_returns_the_expected_representation()
    {
        var some = new Option<int>.Some(42);

        string result = some.ToString();

        result.ShouldBe("Some(42)");
    }

    [Fact]
    public void when_none_to_string_is_called_then_it_returns_the_expected_representation()
    {
        var none = Option<int>.None.Instance;

        string result = none.ToString();

        result.ShouldBe("None");
    }

    [Fact]
    public void when_option_is_compared_to_a_different_type_then_equals_returns_false()
    {
        var option = Option.Some(42);
        string notOption = "not an option";

        bool result = option.Equals(notOption);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_option_is_none_then_filter_returns_none()
    {
        var option = Option.None<int>();

        var result = option.Filter(x => x > 0);

        _ = result.ShouldBeOfType<Option<int>.None>();
    }
}
