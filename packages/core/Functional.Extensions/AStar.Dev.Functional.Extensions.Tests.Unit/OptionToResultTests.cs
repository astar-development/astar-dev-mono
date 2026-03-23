namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class OptionToResultTests
{
    [Fact]
    public void ToResult_FromSome_ReturnsOk()
    {
        var opt    = new Option<int>.Some(42);
        var result = opt.ToResult(() => "missing");
        Assert.True(result is Result<int, string>.Ok ok && ok.Value == 42);
    }

    [Fact]
    public void ToResult_FromNone_ReturnsError()
    {
        var opt    = Option.None<int>();
        var result = opt.ToResult(() => "missing");
        Assert.True(result is Result<int, string>.Error err && err.Reason == "missing");
    }
}
