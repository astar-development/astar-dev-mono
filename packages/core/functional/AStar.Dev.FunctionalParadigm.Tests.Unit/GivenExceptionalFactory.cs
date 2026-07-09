using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalFactory
{
    [Fact]
    public void when_success_created_then_wraps_value()
    {
        var actual = Exceptional.Success(42);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(42));
    }

    [Fact]
    public void when_failure_created_then_wraps_exception()
    {
        var exception = new InvalidOperationException("boom");

        var actual = Exceptional.Failure<int>(exception);

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
    }
}
