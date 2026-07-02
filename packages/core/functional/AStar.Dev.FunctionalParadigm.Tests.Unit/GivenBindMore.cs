using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenBindMore
{
    [Fact]
    public void when_binder_returns_failure_then_returns_that_failure()
    {
        var result = Result.Success<int, string>(3);

        var actual = result.Bind(value => Result.Failure<int, string>($"fail-{value}"));

        actual.ShouldBeOfType<Fail<int, string>>();
        actual.ShouldBe(new Fail<int, string>("fail-3"));
    }
}
