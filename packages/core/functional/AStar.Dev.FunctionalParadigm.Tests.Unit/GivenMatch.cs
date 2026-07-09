using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenMatch
{
    [Fact]
    public void when_on_success_and_on_failure_are_sync_then_returns_expected_values()
    {
        var success = Result.Success<int, string>(4);
        var failure = Result.Failure<int, string>("error");

        var actualSuccess = success.Match(onSuccess: v => v + 1, onFailure: _ => -1);
        var actualFailure = failure.Match(onSuccess: v => v + 1, onFailure: e => e.Length);

        actualSuccess.ShouldBe(5);
        actualFailure.ShouldBe(5);
    }
}
