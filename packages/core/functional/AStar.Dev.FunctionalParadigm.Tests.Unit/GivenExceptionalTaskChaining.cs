using System.Globalization;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalTaskChaining
{
    [Fact]
    public async Task when_run_async_succeeds_then_tap_chained_directly_executes_success_handler()
    {
        bool sideEffect = false;

        var actual = await Try.RunAsync(() => Task.FromResult(7))
            .Tap(value => sideEffect = value == 7);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(7));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_run_async_throws_then_tap_chained_directly_executes_failure_handler()
    {
        var exception = new InvalidOperationException("boom");
        bool sideEffect = false;

        var actual = await Try.RunAsync<int>(() => throw exception)
            .Tap(_ => { }, ex => sideEffect = ex == exception);

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public async Task when_run_async_then_bind_async_then_match_async_chained_directly_on_success_then_returns_expected_value()
    {
        string actual = await Try.RunAsync(() => Task.FromResult(2))
            .BindAsync(value => Task.FromResult(Exceptional.Success(value * 3)))
            .MatchAsync(onSuccess: value => value.ToString(CultureInfo.InvariantCulture), onFailure: ex => ex.Message);

        actual.ShouldBe("6");
    }

    [Fact]
    public async Task when_run_async_then_bind_async_then_match_async_chained_directly_on_failure_then_returns_expected_value()
    {
        var exception = new InvalidOperationException("chain failed");
        bool invoked = false;

        string actual = await Try.RunAsync<int>(() => throw exception)
            .BindAsync(value =>
            {
                invoked = true;

                return Task.FromResult(Exceptional.Success(value * 3));
            })
            .MatchAsync(onSuccess: value => value.ToString(CultureInfo.InvariantCulture), onFailure: ex => ex.Message);

        actual.ShouldBe("chain failed");
        invoked.ShouldBeFalse();
    }
}
