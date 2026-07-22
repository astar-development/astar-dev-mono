namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenExceptionalMatch
{
    [Fact]
    public void when_on_success_then_invokes_success_handler()
    {
        var exceptional = Exceptional.Success(4);

        int actual = exceptional.Match(onSuccess: value => value + 1, onFailure: _ => -1);

        actual.ShouldBe(5);
    }

    [Fact]
    public void when_on_failure_then_invokes_failure_handler()
    {
        var exceptional = Exceptional.Failure<int>(new InvalidOperationException("bad"));

        int actual = exceptional.Match(onSuccess: value => value + 1, onFailure: ex => ex.Message.Length);

        actual.ShouldBe(3);
    }
}
