using AStar.Dev.Logging.Extensions.Models;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(MinimumLevel))]
public class MinimumLevelShould
{
    [Fact]
    public void DefaultProperty_GetsAndSetsValuesCorrectly()
    {
        var minimumLevel = new MinimumLevel();
        var testValue    = "Information";

        minimumLevel.Default = testValue;

        Assert.Equal(testValue, minimumLevel.Default);
    }

    [Fact]
    public void DefaultProperty_DefaultsToEmptyString()
    {
        var minimumLevel = new MinimumLevel();

        Assert.Equal(string.Empty, minimumLevel.Default);
    }

    [Fact]
    public void OverrideProperty_DefaultsToNotNullInstance()
    {
        var minimumLevel = new MinimumLevel();

        Assert.NotNull(minimumLevel.Override);
    }

    [Fact]
    public void OverrideProperty_GetsAndSetsValuesCorrectly()
    {
        var minimumLevel   = new MinimumLevel();
        var customOverride = new Override { MicrosoftAspNetCore = "Warning", SystemNetHttp = "Error", AStar = "Debug" };

        minimumLevel.Override = customOverride;

        Assert.Equal(customOverride, minimumLevel.Override);
        Assert.Equal("Warning",      minimumLevel.Override.MicrosoftAspNetCore);
        Assert.Equal("Error",        minimumLevel.Override.SystemNetHttp);
        Assert.Equal("Debug",        minimumLevel.Override.AStar);
    }

    [Fact]
    public void OverrideProperty_InstanceHasDefaultValues()
    {
        var minimumLevel = new MinimumLevel();

        var overrideInstance = minimumLevel.Override;

        Assert.Equal(string.Empty, overrideInstance.MicrosoftAspNetCore);
        Assert.Equal(string.Empty, overrideInstance.SystemNetHttp);
        Assert.Equal(string.Empty, overrideInstance.AStar);
    }
}