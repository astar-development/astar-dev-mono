using AStar.Dev.Utilities;
using LogLevel = AStar.Dev.Logging.Extensions.Models.LogLevel;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(LogLevel))]
public class LogLevelShould
{
    [Fact]
    public void Default_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new LogLevel();

        logLevel.Default.ShouldNotBeNull();
        logLevel.Default.ShouldBe(string.Empty);
    }

    [Fact]
    public void Default_ShouldAllowSettingValue()
    {
        var logLevel      = new LogLevel();
        var expectedValue = "Info";

        logLevel.Default = expectedValue;

        logLevel.Default.ShouldBe(expectedValue);
    }

    [Fact]
    public void MicrosoftAspNetCore_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new LogLevel();

        logLevel.MicrosoftAspNetCore.ShouldNotBeNull();
        logLevel.MicrosoftAspNetCore.ShouldBe(string.Empty);
    }

    [Fact]
    public void MicrosoftAspNetCore_ShouldAllowSettingValue()
    {
        var logLevel      = new LogLevel();
        var expectedValue = "Warning";

        logLevel.MicrosoftAspNetCore = expectedValue;

        logLevel.MicrosoftAspNetCore.ShouldBe(expectedValue);
    }

    [Fact]
    public void AStar_ShouldHaveInitialValue_EmptyString()
    {
        var logLevel = new LogLevel();

        logLevel.AStar.ShouldNotBeNull();
        logLevel.AStar.ShouldBe(string.Empty);
    }

    [Fact]
    public void AStar_ShouldAllowSettingValue()
    {
        var logLevel      = new LogLevel();
        var expectedValue = "Error";

        logLevel.AStar = expectedValue;

        logLevel.AStar.ShouldBe(expectedValue);
    }

    [Fact]
    public void ToString_ShouldListAllProperties()
    {
        var logLevel = new LogLevel { Default = "Debug", MicrosoftAspNetCore = "Information", AStar = "Error" };

        logLevel.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void Equals_ShouldReturnTrueForIdenticalValues()
    {
        var logLevel1 = new LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };
        var logLevel2 = new LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };

        logLevel1.ToJson().ShouldBeEquivalentTo(logLevel2.ToJson());
    }

    [Fact]
    public void Equals_ShouldReturnFalseForDifferentValues()
    {
        var logLevel1 = new LogLevel { Default = "Info", MicrosoftAspNetCore = "Debug", AStar = "Trace" };
        var logLevel2 = new LogLevel { Default = "Warn", MicrosoftAspNetCore = "Error", AStar = "Fatal" };

        logLevel1.Equals(logLevel2).ShouldBeFalse();
    }
}