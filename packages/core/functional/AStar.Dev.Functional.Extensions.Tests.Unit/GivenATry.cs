namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public sealed class GivenATry
{
    [Fact]
    public void when_run_succeeds_then_the_result_captures_the_returned_value()
    {
        var result = Try.Run(() => 42);

        int output = result.Match(
                                  ok => ok,
                                  ex => -1);

        Assert.Equal(42, output);
    }

    [Fact]
    public void when_run_throws_then_the_result_captures_the_exception()
    {
        var result = Try.Run<int>(() => throw new InvalidOperationException("fail"));

        int output = result.Match(
                                  ok => ok,
                                  ex => -1);

        Assert.Equal(-1, output);
    }

    [Fact]
    public void when_matching_a_success_or_a_failure_then_the_correct_branch_is_invoked()
    {
        var success = Try.Run(() => "done");
        var failure = Try.Run<string>(() => throw new InvalidOperationException("fail"));

        string a = success.Match(x => $"OK: {x}", ex => $"ERR: {ex.Message}");
        string b = failure.Match(x => $"OK: {x}", ex => $"ERR: {ex.Message}");

        Assert.Equal("OK: done", a);
        Assert.Equal("ERR: fail", b);
    }
}
