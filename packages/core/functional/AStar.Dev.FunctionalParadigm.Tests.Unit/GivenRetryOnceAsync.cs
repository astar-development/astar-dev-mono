using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public sealed class GivenRetryOnceAsync
{
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
        var attempts = 0;

        await RetryExtensions.RetryOnceAsync<int, string>(
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
        var retryInvoked = false;

        await RetryExtensions.RetryOnceAsync<int, string>(
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
        var attempts = 0;

        var actual = await RetryExtensions.RetryOnceAsync<int, string>(
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
        var attempts = 0;

        await RetryExtensions.RetryOnceAsync<int, string>(
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
        var retryInvocations = 0;
        var attempts = 0;

        await RetryExtensions.RetryOnceAsync<int, string>(
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
        var attempts = 0;

        var actual = await RetryExtensions.RetryOnceAsync<int, string>(
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
        var attempts = 0;

        await RetryExtensions.RetryOnceAsync<int, string>(
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
        var retryInvocations = 0;

        await RetryExtensions.RetryOnceAsync<int, string>(
            () => Task.FromResult(Result.Failure<int, string>("failed")),
            () =>
            {
                retryInvocations++;

                return Task.CompletedTask;
            });

        retryInvocations.ShouldBe(1);
    }
}
