namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalBind
{
    [Fact]
    public void when_exceptional_is_success_then_returns_bound_result()
    {
        var exceptional = Exceptional.Success(2);

        var actual = exceptional.Bind(value => Exceptional.Success(value * 5));

        actual.ShouldBeOfType<Success<int>>();
        actual.ShouldBe(new Success<int>(10));
    }

    [Fact]
    public void when_binder_returns_failure_then_returns_that_failure()
    {
        var exceptional = Exceptional.Success(3);
        var exception = new InvalidOperationException("nope");

        var actual = exceptional.Bind(_ => Exceptional.Failure<int>(exception));

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
    }

    [Fact]
    public void when_exceptional_is_failure_then_binder_is_not_invoked()
    {
        var exception = new InvalidOperationException("err");
        var exceptional = Exceptional.Failure<int>(exception);
        bool invoked = false;

        var actual = exceptional.Bind(value =>
        {
            invoked = true;

            return Exceptional.Success(value * 5);
        });

        actual.ShouldBeOfType<Failure<int>>();
        actual.ShouldBe(new Failure<int>(exception));
        invoked.ShouldBeFalse();
    }
}
