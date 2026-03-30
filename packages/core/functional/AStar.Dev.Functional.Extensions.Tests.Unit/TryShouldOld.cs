namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class TryShouldOld
{
    [Fact]
    public void Try_Run_CapturesSuccess()
    {
        var result = Try.Run(() => 42);

        int output = result.Match(
                                  ok => ok,
                                  ex => -1);

        Assert.Equal(42, output);
    }

    [Fact]
    public void Try_Run_CapturesException()
    {
        var result = Try.Run<int>(() => throw new InvalidOperationException("fail"));

        int output = result.Match(
                                  ok => ok,
                                  ex => -1);

        Assert.Equal(-1, output);
    }

    [Fact]
    public void Try_Match_ReturnsCorrectBranch()
    {
        var success = Try.Run(() => "done");
        var failure = Try.Run<string>(() => throw new InvalidOperationException("fail"));

        string a = success.Match(x => $"OK: {x}", ex => $"ERR: {ex.Message}");
        string b = failure.Match(x => $"OK: {x}", ex => $"ERR: {ex.Message}");

        Assert.Equal("OK: done",  a);
        Assert.Equal("ERR: fail", b);
    }
}
