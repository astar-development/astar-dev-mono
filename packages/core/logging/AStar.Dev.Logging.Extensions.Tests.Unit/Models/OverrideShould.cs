// C:\repos\M\astar-dev-logging-extensions\tests\AStar.Dev.Logging.Extensions.Tests.Unit\Models\OverrideTest.cs

using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Override))]
public class OverrideShould
{
    [Fact]
    public void MicrosoftAspNetCore_DefaultValue_ShouldBeEmptyString()
    {
        var overrideInstance = new Override();

        var result = overrideInstance.MicrosoftAspNetCore;

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Information")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("")]
    public void MicrosoftAspNetCore_ShouldAllowSettingValidValues(string value)
    {
        var overrideInstance = new Override { MicrosoftAspNetCore = value };

        Assert.Equal(value, overrideInstance.MicrosoftAspNetCore);
    }

    [Fact]
    public void SystemNetHttp_DefaultValue_ShouldBeEmptyString()
    {
        var overrideInstance = new Override();

        var result = overrideInstance.SystemNetHttp;

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Trace")]
    [InlineData("")]
    public void SystemNetHttp_ShouldAllowSettingValidValues(string value)
    {
        var overrideInstance = new Override { SystemNetHttp = value };

        Assert.Equal(value, overrideInstance.SystemNetHttp);
    }

    [Fact]
    public void AStar_DefaultValue_ShouldBeEmptyString()
    {
        var overrideInstance = new Override();

        var result = overrideInstance.AStar;

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Critical")]
    [InlineData("Fatal")]
    [InlineData("")]
    public void AStar_ShouldAllowSettingValidValues(string value)
    {
        var overrideInstance = new Override { AStar = value };

        Assert.Equal(value, overrideInstance.AStar);
    }

    [Fact]
    public void Should_CreateNewInstanceWithDefaults()
    {
        var overrideInstance = new Override();

        Assert.NotNull(overrideInstance);
        Assert.Equal(string.Empty, overrideInstance.MicrosoftAspNetCore);
        Assert.Equal(string.Empty, overrideInstance.SystemNetHttp);
        Assert.Equal(string.Empty, overrideInstance.AStar);
    }
}