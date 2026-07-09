using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalTap
{
    [Fact]
    public void when_exceptional_is_success_then_executes_success_handler_and_returns_same()
    {
        var exceptional = Exceptional.Success(7);
        var sideEffect = false;

        var actual = exceptional.Tap(value => sideEffect = value == 7);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(7));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public void when_exceptional_is_failure_then_executes_failure_handler_and_returns_same()
    {
        var exception = new InvalidOperationException("oops");
        var exceptional = Exceptional.Failure<int>(exception);
        var sideEffect = false;

        var actual = exceptional.Tap(_ => { }, ex => sideEffect = ex == exception);

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
        sideEffect.ShouldBeTrue();
    }
}
