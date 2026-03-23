// C:\repos\M\astar-dev-logging-extensions\tests\AStar.Dev.Logging.Extensions.Tests.Unit\Models\ApplicationInsightsTest.cs

using AStar.Dev.Logging.Extensions.Models;
using LogLevel = AStar.Dev.Logging.Extensions.Models.LogLevel;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(ApplicationInsights))]
public class ApplicationInsightsShould
{
    [Fact]
    public void ApplicationInsights_DefaultConstructor_ShouldInitializeLogLevel()
    {
        var applicationInsights = new ApplicationInsights();

        applicationInsights.LogLevel.ShouldNotBeNull();
        applicationInsights.LogLevel.ShouldBeOfType<LogLevel>();
    }

    [Fact]
    public void ApplicationInsights_LogLevel_ShouldDefaultToEmptyValues()
    {
        var applicationInsights = new ApplicationInsights();

        var logLevel = applicationInsights.LogLevel;

        logLevel.Default.ShouldBe(string.Empty);
        logLevel.MicrosoftAspNetCore.ShouldBe(string.Empty);
        logLevel.AStar.ShouldBe(string.Empty);
    }

    [Fact]
    public void ApplicationInsights_ShouldAllowModifyingLogLevelProperty()
    {
        var applicationInsights = new ApplicationInsights();

        var customLogLevel = new LogLevel { Default = "Information", MicrosoftAspNetCore = "Warning", AStar = "Debug" };
        applicationInsights.LogLevel = customLogLevel;

        applicationInsights.LogLevel.Default.ShouldBe("Information");
        applicationInsights.LogLevel.MicrosoftAspNetCore.ShouldBe("Warning");
        applicationInsights.LogLevel.AStar.ShouldBe("Debug");
    }

    [Fact]
    public void ApplicationInsights_LogLevel_ShouldSupportEdgeCases()
    {
        var applicationInsights = new ApplicationInsights();

        var customLogLevel = new LogLevel { Default = null!, MicrosoftAspNetCore = "", AStar = new('A', 1000) };
        applicationInsights.LogLevel = customLogLevel;

        applicationInsights.LogLevel.Default.ShouldBeNull();
        applicationInsights.LogLevel.MicrosoftAspNetCore.ShouldBe(string.Empty);
        applicationInsights.LogLevel.AStar.ShouldBe(new('A', 1000));
    }
}