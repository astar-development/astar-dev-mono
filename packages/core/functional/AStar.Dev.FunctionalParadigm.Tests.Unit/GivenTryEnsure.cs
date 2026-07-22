namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenTryEnsure
{
    [Fact]
    public void when_the_exceptional_is_a_success_then_the_finally_action_is_invoked_and_the_original_result_is_returned()
    {
        bool finallyActionInvoked = false;
        var success = new Success<int>(42);

        var actual = success.Ensure(() => finallyActionInvoked = true);

        finallyActionInvoked.ShouldBeTrue();
        actual.ShouldBe(success);
    }

    [Fact]
    public void when_the_exceptional_is_a_failure_then_the_finally_action_is_invoked_and_the_original_result_is_returned()
    {
        bool finallyActionInvoked = false;
        var failure = new Failure<int>(new InvalidOperationException("boom"));

        var actual = failure.Ensure(() => finallyActionInvoked = true);

        finallyActionInvoked.ShouldBeTrue();
        actual.ShouldBe(failure);
    }
}
