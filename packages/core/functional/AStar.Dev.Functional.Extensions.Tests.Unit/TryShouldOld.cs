namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class TryShouldOld
{
    [Fact]
    public void Try_Run_CapturesSuccess()
    {
        Result<int, Exception> result = Try.Run(() => 42);

        int output = result.Match(
                                  ok => ok,
                                  ex => -1);

        Assert.Equal(42, output);
    }

    [Fact]
    public void Try_Run_CapturesException()
    {
        Result<int, Exception> result = Try.Run<int>(() => throw new InvalidOperationException("fail"));

        int output = result.Match(
                                  ok => ok,
                                  ex => -1);

        Assert.Equal(-1, output);
    }

    [Fact]
    public void Try_Match_ReturnsCorrectBranch()
    {
        Result<string, Exception> success = Try.Run(() => "done");
        Result<string, Exception> failure = Try.Run<string>(() => throw new InvalidOperationException("fail"));

        string a = success.Match(x => $"OK: {x}", ex => $"ERR: {ex.Message}");
        string b = failure.Match(x => $"OK: {x}", ex => $"ERR: {ex.Message}");

        Assert.Equal("OK: done",  a);
        Assert.Equal("ERR: fail", b);
    }
}
