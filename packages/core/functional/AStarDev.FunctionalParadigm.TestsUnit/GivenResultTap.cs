namespace AStar.Dev.FunctionalParadigm.TestsUnit;

public sealed class GivenResultTap
{
    [Fact]
    public void when_tap_error_is_called_on_a_success_then_the_failure_handler_is_not_invoked_and_the_same_result_is_returned()
    {
        var result = Result.Success<int, string>(3);
        bool failureHandlerInvoked = false;

        var actual = result.TapError(_ => failureHandlerInvoked = true);

        actual.ShouldBe(new Ok<int, string>(3));
        failureHandlerInvoked.ShouldBeFalse();
    }

    [Fact]
    public void when_tap_error_is_called_on_a_failure_then_the_failure_handler_is_invoked_with_the_error_and_the_same_result_is_returned()
    {
        var result = Result.Failure<int, string>("boom");
        string? receivedError = null;

        var actual = result.TapError(error => receivedError = error);

        actual.ShouldBe(new Fail<int, string>("boom"));
        receivedError.ShouldBe("boom");
    }

    [Fact]
    public async Task when_result_tap_async_is_called_on_a_success_then_the_async_handler_is_invoked_and_the_same_result_is_returned()
    {
        var result = Result.Success<int, string>(9);
        int? receivedValue = null;

        var actual = await result.TapAsync(value =>
        {
            receivedValue = value;

            return Task.CompletedTask;
        });

        actual.ShouldBe(new Ok<int, string>(9));
        receivedValue.ShouldBe(9);
    }

    [Fact]
    public async Task when_result_tap_async_is_called_on_a_failure_then_the_async_handler_is_not_invoked_and_the_same_failure_is_returned()
    {
        var result = Result.Failure<int, string>("error");
        bool handlerInvoked = false;

        var actual = await result.TapAsync(_ =>
        {
            handlerInvoked = true;

            return Task.CompletedTask;
        });

        actual.ShouldBe(new Fail<int, string>("error"));
        handlerInvoked.ShouldBeFalse();
    }
}
