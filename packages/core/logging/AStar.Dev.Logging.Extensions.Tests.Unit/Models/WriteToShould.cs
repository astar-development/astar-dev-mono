using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(WriteTo))]
public class WriteToShould
{
    [Fact]
    public void InitializeName_AsEmptyStringByDefault()
    {
        var writeTo = new WriteTo();

        writeTo.Name.ShouldNotBeNull();
        writeTo.Name.ShouldBe(string.Empty);
    }

    [Fact]
    public void InitializeArgs_WithNewInstanceByDefault()
    {
        var writeTo = new WriteTo();

        writeTo.Args.ShouldNotBeNull();
        writeTo.Args.ShouldBeOfType<Args>();
        writeTo.Args.ServerUrl.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("File")]
    [InlineData("Console")]
    [InlineData("")]
    public void SetValidValues_ForNameProperty(string name)
    {
        var writeTo = new WriteTo { Name = name };

        writeTo.Name.ShouldBe(name);
    }

    [Fact]
    public void AllowSetting_ArgsPropertyWithNonNullValues()
    {
        var writeTo = new WriteTo();
        var newArgs = new Args { ServerUrl = "http://example.com" };

        writeTo.Args = newArgs;

        writeTo.Args.ShouldBe(newArgs);
        writeTo.Args.ServerUrl.ShouldBe("http://example.com");
    }
}