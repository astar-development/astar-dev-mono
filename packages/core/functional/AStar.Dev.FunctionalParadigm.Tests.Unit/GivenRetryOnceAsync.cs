namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public sealed class GivenRetryOnceAsync
{
    [Fact]
    public async Task when_operation_is_null_then_throws_argument_null_exception()
    {
        Func<Task> retryOnceAction = () => RetryExtensions.RetryOnceAsync<int, string>(null!, () => Task.CompletedTask);

        (await retryOnceAction.ShouldThrowAsync<ArgumentNullException>()).ParamName.ShouldBe("operation");
    }

    [Fact]
    public async Task when_on_retry_is_null_then_throws_argument_null_exception()
    {
        Func<Task> retryOnceAction = () => RetryExtensions.RetryOnceAsync(() => Task.FromResult(Result.Success<int, string>(1)), null!);

        (await retryOnceAction.ShouldThrowAsync<ArgumentNullException>()).ParamName.ShouldBe("onRetry");
    }

    [Fact]
    public async Task when_the_first_attempt_succeeds_then_the_result_is_the_first_attempts_success()
    {
        var actual = await RetryExtensions.RetryOnceAsync(
            () => Task.FromResult(Result.Success<int, string>(1)),
            () => Task.CompletedTask);

        actual.ShouldBe(new Ok<int, string>(1));
    }

    [Fact]
    public async Task when_the_first_attempt_succeeds_then_the_operation_is_invoked_exactly_once()
    {
        int attempts = 0;

        await RetryExtensions.RetryOnceAsync(
            () =>
            {
                attempts++;

                return Task.FromResult(Result.Success<int, string>(1));
            },
            () => Task.CompletedTask);

        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_first_attempt_succeeds_then_the_retry_callback_is_never_invoked()
    {
        bool retryInvoked = false;

        await RetryExtensions.RetryOnceAsync(
            () => Task.FromResult(Result.Success<int, string>(1)),
            () =>
            {
                retryInvoked = true;

                return Task.CompletedTask;
            });

        retryInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_first_attempt_fails_and_the_second_succeeds_then_the_result_is_the_second_attempts_success()
    {
        int attempts = 0;

        var actual = await RetryExtensions.RetryOnceAsync(
            () =>
            {
                attempts++;

                return Task.FromResult(attempts == 1 ? Result.Failure<int, string>("first-failed") : Result.Success<int, string>(2));
            },
            () => Task.CompletedTask);

        actual.ShouldBe(new Ok<int, string>(2));
    }

    [Fact]
    public async Task when_the_first_attempt_fails_and_the_second_succeeds_then_the_operation_is_invoked_exactly_twice()
    {
        int attempts = 0;

        await RetryExtensions.RetryOnceAsync(
            () =>
            {
                attempts++;

                return Task.FromResult(attempts == 1 ? Result.Failure<int, string>("first-failed") : Result.Success<int, string>(2));
            },
            () => Task.CompletedTask);

        attempts.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_first_attempt_fails_and_the_second_succeeds_then_the_retry_callback_is_invoked_exactly_once()
    {
        int retryInvocations = 0;
        int attempts = 0;

        await RetryExtensions.RetryOnceAsync(
            () =>
            {
                attempts++;

                return Task.FromResult(attempts == 1 ? Result.Failure<int, string>("first-failed") : Result.Success<int, string>(2));
            },
            () =>
            {
                retryInvocations++;

                return Task.CompletedTask;
            });

        retryInvocations.ShouldBe(1);
    }

    [Fact]
    public async Task when_both_attempts_fail_then_the_result_is_the_second_attempts_failure()
    {
        int attempts = 0;

        var actual = await RetryExtensions.RetryOnceAsync(
            () =>
            {
                attempts++;

                return Task.FromResult(Result.Failure<int, string>($"failed-{attempts}"));
            },
            () => Task.CompletedTask);

        actual.ShouldBe(new Fail<int, string>("failed-2"));
    }

    [Fact]
    public async Task when_both_attempts_fail_then_the_operation_is_invoked_exactly_twice()
    {
        int attempts = 0;

        await RetryExtensions.RetryOnceAsync(
            () =>
            {
                attempts++;

                return Task.FromResult(Result.Failure<int, string>($"failed-{attempts}"));
            },
            () => Task.CompletedTask);

        attempts.ShouldBe(2);
    }

    [Fact]
    public async Task when_both_attempts_fail_then_the_retry_callback_is_invoked_exactly_once()
    {
        int retryInvocations = 0;

        await RetryExtensions.RetryOnceAsync(
            () => Task.FromResult(Result.Failure<int, string>("failed")),
            () =>
            {
                retryInvocations++;

                return Task.CompletedTask;
            });

        retryInvocations.ShouldBe(1);
    }
}
