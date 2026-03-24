using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Args))]
public class ArgsShould
{
    [Fact]
    public void DefaultToEmptyString_WhenServerUrlIsUnset()
    {
        var args = new Args();

        args.ServerUrl.ShouldNotBeNull();
        args.ServerUrl.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://myserver.local")]
    [InlineData("")]
    public void SupportSetAndGet_ForServerUrl(string input)
    {
        var args = new Args { ServerUrl = input };

        args.ServerUrl.ShouldBe(input);
    }

    [Fact]
    public void AcceptEmptyString_ForServerUrl()
    {
        var args = new Args { ServerUrl = string.Empty };

        args.ServerUrl.ShouldBe(string.Empty);
    }
}