using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenTapSync
{
    [Fact]
    public void when_called_on_result_success_then_executes_success_handler_and_returns_same()
    {
        var result = Result.Success<int, string>(7);
        var sideEffect = false;

        var actual = result.Tap(value => sideEffect = value == 7);

        actual.ShouldBeOfType<Ok<int, string>>();
        actual.ShouldBe(new Ok<int, string>(7));
        sideEffect.ShouldBeTrue();
    }

    [Fact]
    public void when_called_on_result_failure_then_executes_failure_handler_and_returns_failure()
    {
        var result = Result.Failure<int, string>("oops");
        var sideEffect = false;

        var actual = result.Tap(_ => { }, error => sideEffect = error == "oops");

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("oops"));
        sideEffect.ShouldBeTrue();
    }
}
