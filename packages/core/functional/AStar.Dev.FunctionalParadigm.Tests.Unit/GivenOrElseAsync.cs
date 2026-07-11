using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenOrElseAsync
{
    [Fact]
    public async Task when_result_is_success_then_returns_original_without_invoking_fallback()
    {
        var resultTask = Task.FromResult(Result.Success<int, string>(42));
        bool invoked = false;

        var actual = await resultTask.OrElseAsync(_ =>
        {
            invoked = true;

            return Task.FromResult(Result.Success<int, string>(0));
        });

        actual.ShouldBe(new Ok<int, string>(42));
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_result_is_failure_then_returns_fallback_result()
    {
        var resultTask = Task.FromResult(Result.Failure<int, string>("first failed"));

        var actual = await resultTask.OrElseAsync(_ => Task.FromResult(Result.Success<int, string>(7)));

        actual.ShouldBe(new Ok<int, string>(7));
    }

    [Fact]
    public async Task when_result_is_failure_then_fallback_receives_original_error()
    {
        var resultTask = Task.FromResult(Result.Failure<int, string>("original error"));
        string? receivedError = null;

        _ = await resultTask.OrElseAsync(error =>
        {
            receivedError = error;

            return Task.FromResult(Result.Success<int, string>(1));
        });

        receivedError.ShouldBe("original error");
    }

    [Fact]
    public async Task when_result_and_fallback_both_fail_then_returns_fallback_failure()
    {
        var resultTask = Task.FromResult(Result.Failure<int, string>("first failed"));

        var actual = await resultTask.OrElseAsync(_ => Task.FromResult(Result.Failure<int, string>("fallback failed")));

        actual.ShouldBe(new Fail<int, string>("fallback failed"));
    }
}
