using AStar.Dev.Logging.Extensions.Models;
using AStar.Dev.Utilities;
using Console = AStar.Dev.Logging.Extensions.Models.Console;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(SerilogConfig))]
public class SerilogConfigShould
{
    [Fact]
    public void SerilogConfig_ShouldInitializeWithDefaultValues()
    {
        var config = new SerilogConfig();

        config.Serilog.ShouldNotBeNull();
        config.Logging.ShouldNotBeNull();
    }

    [Fact]
    public void SerilogConfig_ShouldAllowSettingProperties()
    {
        var serilogConfig = new SerilogConfig();

        var serilog = new Extensions.Models.Serilog
                      {
                          Enrich       = new[] { "ThreadId", "MachineName" },
                          WriteTo      = new[] { new WriteTo { Name                                              = "File", Args             = new() { ServerUrl = "http://localhost" } } },
                          MinimumLevel = new() { Default = "Information", Override = new() { MicrosoftAspNetCore = "Warning", SystemNetHttp = "Error", AStar = "Debug" } }
                      };

        var logging = new Extensions.Models.Logging
                      {
                          Console = new()
                                    {
                                        FormatterName = "default",
                                        FormatterOptions = new()
                                                           {
                                                               SingleLine        = true,
                                                               IncludeScopes     = false,
                                                               TimestampFormat   = "yyyy-MM-dd",
                                                               UseUtcTimestamp   = false,
                                                               JsonWriterOptions = new()
                                                           }
                                    },
                          ApplicationInsights = new() { LogLevel = new() { Default = "Debug", MicrosoftAspNetCore = "Trace", AStar = "Error" } }
                      };

        serilogConfig.Serilog = serilog;
        serilogConfig.Logging = logging;

        serilogConfig.Serilog.ShouldBeEquivalentTo(serilog);
        serilogConfig.Logging.ShouldBeEquivalentTo(logging);
    }

    [Fact]
    public void Serilog_ShouldInitializeWithDefaultValues()
    {
        var serilog = new Extensions.Models.Serilog();

        serilog.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void Logging_ShouldInitializeWithDefaultValues()
    {
        var logging = new Extensions.Models.Logging();

        logging.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void WriteTo_ShouldInitializeWithDefaultValues()
    {
        var writeTo = new WriteTo();

        writeTo.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void MinimumLevel_ShouldInitializeWithDefaultValues()
    {
        var minimumLevel = new MinimumLevel();

        minimumLevel.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void Console_ShouldInitializeWithDefaultValues()
    {
        var console = new Console();

        console.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void ApplicationInsights_ShouldInitializeWithDefaultValues()
    {
        var applicationInsights = new ApplicationInsights();

        applicationInsights.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void FormatterOptions_ShouldInitializeWithDefaultValues()
    {
        var formatterOptions = new FormatterOptions();

        formatterOptions.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void JsonWriterOptions_ShouldInitializeSuccessfully()
    {
        var jsonWriterOptions = new JsonWriterOptions();

        jsonWriterOptions.ToJson().ShouldMatchApproved();
    }
}