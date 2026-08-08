namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public sealed class GivenAResultWithExtensionsApplied
{
    [Fact]
    public void when_result_is_ok_then_map_transforms_the_success_value()
    {
        var result = new Result<int, string>.Ok(42);

        var mapped = result.Map(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       err => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void when_result_is_error_then_map_preserves_the_error()
    {
        var result = new Result<int, string>.Error("error message");

        var mapped = result.Map(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task when_result_is_ok_then_map_async_transforms_the_success_value()
    {
        var result = new Result<int, string>.Ok(42);

        var mapped = await result.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_result_is_error_then_map_async_preserves_the_error()
    {
        var result = new Result<int, string>.Error("error message");

        var mapped = await result.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task when_task_result_is_ok_then_map_async_transforms_the_success_value()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var mapped = await resultTask.MapAsync(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_task_result_is_error_then_map_async_preserves_the_error()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error message"));

        var mapped = await resultTask.MapAsync(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task when_task_result_is_ok_then_map_async_with_async_mapper_transforms_the_success_value()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var mapped = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_task_result_is_error_then_map_async_with_async_mapper_preserves_the_error()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error message"));

        var mapped = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public void when_result_is_error_then_map_failure_transforms_the_error_value()
    {
        var result = new Result<string, int>.Error(42);

        var mapped = result.MapFailure(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void when_result_is_ok_then_map_failure_preserves_the_success_value()
    {
        var result = new Result<string, int>.Ok("success");

        var mapped = result.MapFailure(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task when_result_is_error_then_map_failure_async_transforms_the_error_value()
    {
        var result = new Result<string, int>.Error(42);

        var mapped = await result.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_result_is_ok_then_map_failure_async_preserves_the_success_value()
    {
        var result = new Result<string, int>.Ok("success");

        var mapped = await result.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task when_task_result_is_error_then_map_failure_async_transforms_the_error_value()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));

        var mapped = await resultTask.MapFailureAsync(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_task_result_is_ok_then_map_failure_async_preserves_the_success_value()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));

        var mapped = await resultTask.MapFailureAsync(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task when_task_result_is_error_then_map_failure_async_with_async_mapper_transforms_the_error_value()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));

        var mapped = await resultTask.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = mapped.Match(
                                       _ => throw new InvalidOperationException("Should not be success"),
                                       err => err
                                      );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_task_result_is_ok_then_map_failure_async_with_async_mapper_preserves_the_success_value()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));

        var mapped = await resultTask.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = mapped.Match(
                                       ok => ok,
                                       _ => throw new InvalidOperationException("Should not be error")
                                      );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public void when_result_is_ok_then_bind_returns_the_new_result()
    {
        var result = new Result<int, string>.Ok(42);

        var bound = result.Bind(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void when_bind_function_returns_an_error_result_then_bind_returns_the_error_result()
    {
        var result = new Result<int, string>.Ok(42);

        var bound = result.Bind(value => new Result<string, string>.Error("bound error"));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public void when_result_is_error_then_bind_preserves_the_original_error()
    {
        var result = new Result<int, string>.Error("original error");

        var bound = result.Bind(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task when_result_is_ok_then_bind_async_returns_the_new_result()
    {
        var result = new Result<int, string>.Ok(42);

        var bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_async_bind_function_returns_an_error_result_then_bind_async_returns_the_error_result()
    {
        var result = new Result<int, string>.Ok(42);

        var bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Error("bound error")));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task when_result_is_error_then_bind_async_preserves_the_original_error()
    {
        var result = new Result<int, string>.Error("original error");

        var bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task when_task_result_is_ok_then_bind_async_returns_the_new_result()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var bound = await resultTask.BindAsync(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_task_result_is_ok_and_bind_function_returns_an_error_result_then_bind_async_returns_the_error_result()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var bound = await resultTask.BindAsync(value => new Result<string, string>.Error("bound error"));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task when_task_result_is_error_then_bind_async_preserves_the_original_error()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("original error"));

        var bound = await resultTask.BindAsync(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task when_task_result_is_ok_then_bind_async_with_async_binder_returns_the_new_result()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        string matchResult = bound.Match(
                                      ok => ok,
                                      _ => throw new InvalidOperationException("Should not be error")
                                     );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task when_task_result_is_ok_and_async_bind_function_returns_an_error_result_then_bind_async_returns_the_error_result()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Error("bound error")));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task when_task_result_is_error_then_bind_async_with_async_binder_preserves_the_original_error()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("original error"));

        var bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        string matchResult = bound.Match(
                                      _ => throw new InvalidOperationException("Should not be success"),
                                      err => err
                                     );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public void when_result_is_ok_then_tap_executes_the_side_effect_and_returns_the_original_result()
    {
        var result = new Result<int, string>.Ok(42);
        int sideEffectValue = 0;

        var tapped = result.Tap(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void when_result_is_error_then_tap_does_not_execute_the_side_effect_and_returns_the_original_result()
    {
        var result = new Result<int, string>.Error("error");
        int sideEffectValue = 0;

        var tapped = result.Tap(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void when_result_is_error_then_tap_error_executes_the_side_effect_and_returns_the_original_result()
    {
        var result = new Result<string, int>.Error(42);
        int sideEffectValue = 0;

        var tapped = result.TapError(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void when_result_is_ok_then_tap_error_does_not_execute_the_side_effect_and_returns_the_original_result()
    {
        var result = new Result<string, int>.Ok("success");
        int sideEffectValue = 0;

        var tapped = result.TapError(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task when_result_is_ok_then_tap_async_executes_the_side_effect_and_returns_the_original_result()
    {
        var result = new Result<int, string>.Ok(42);
        int sideEffectValue = 0;

        var tapped = await result.TapAsync(value =>
                                           {
                                               sideEffectValue = value;

                                               return Task.CompletedTask;
                                           });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task when_result_is_error_then_tap_async_does_not_execute_the_side_effect_and_returns_the_original_result()
    {
        var result = new Result<int, string>.Error("error");
        int sideEffectValue = 0;

        var tapped = await result.TapAsync(value =>
                                           {
                                               sideEffectValue = value;

                                               return Task.CompletedTask;
                                           });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task when_result_is_error_then_tap_error_async_executes_the_side_effect_and_returns_the_original_result()
    {
        var result = new Result<string, int>.Error(42);
        int sideEffectValue = 0;

        var tapped = await result.TapErrorAsync(value =>
                                                {
                                                    sideEffectValue = value;

                                                    return Task.CompletedTask;
                                                });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task when_result_is_ok_then_tap_error_async_does_not_execute_the_side_effect_and_returns_the_original_result()
    {
        var result = new Result<string, int>.Ok("success");
        int sideEffectValue = 0;

        var tapped = await result.TapErrorAsync(value =>
                                                {
                                                    sideEffectValue = value;

                                                    return Task.CompletedTask;
                                                });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task when_task_result_is_ok_then_tap_async_executes_the_side_effect_and_returns_the_original_result()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));
        int sideEffectValue = 0;

        var tapped = await resultTask.TapAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task when_task_result_is_error_then_tap_async_does_not_execute_the_side_effect_and_returns_the_original_result()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error"));
        int sideEffectValue = 0;

        var tapped = await resultTask.TapAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task when_task_result_is_error_then_tap_error_async_executes_the_side_effect_and_returns_the_original_result()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));
        int sideEffectValue = 0;

        var tapped = await resultTask.TapErrorAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task when_task_result_is_ok_then_tap_error_async_does_not_execute_the_side_effect_and_returns_the_original_result()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));
        int sideEffectValue = 0;

        var tapped = await resultTask.TapErrorAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task when_task_result_is_ok_then_tap_async_with_async_action_executes_the_side_effect_and_returns_the_original_result()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));
        int sideEffectValue = 0;

        var tapped = await resultTask.TapAsync(value =>
                                               {
                                                   sideEffectValue = value;

                                                   return Task.CompletedTask;
                                               });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task when_task_result_is_error_then_tap_async_with_async_action_does_not_execute_the_side_effect_and_returns_the_original_result()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error"));
        int sideEffectValue = 0;

        var tapped = await resultTask.TapAsync(value =>
                                               {
                                                   sideEffectValue = value;

                                                   return Task.CompletedTask;
                                               });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task when_task_result_is_error_then_tap_error_async_with_async_action_executes_the_side_effect_and_returns_the_original_result()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));
        int sideEffectValue = 0;

        var tapped = await resultTask.TapErrorAsync(value =>
                                                    {
                                                        sideEffectValue = value;

                                                        return Task.CompletedTask;
                                                    });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task when_task_result_is_ok_then_tap_error_async_with_async_action_does_not_execute_the_side_effect_and_returns_the_original_result()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));
        int sideEffectValue = 0;

        var tapped = await resultTask.TapErrorAsync(value =>
                                                    {
                                                        sideEffectValue = value;

                                                        return Task.CompletedTask;
                                                    });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }
}
