namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalMap
{
    [Fact]
    public void when_exceptional_is_success_then_transforms_value()
    {
        var exceptional = Exceptional.Success(5);

        var actual = exceptional.Map(value => value * 3);

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(15));
    }

    [Fact]
    public void when_exceptional_is_failure_then_returns_same_failure()
    {
        var exception = new InvalidOperationException("failed");
        var exceptional = Exceptional.Failure<int>(exception);

        var actual = exceptional.Map(value => value * 3);

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
    }
}
