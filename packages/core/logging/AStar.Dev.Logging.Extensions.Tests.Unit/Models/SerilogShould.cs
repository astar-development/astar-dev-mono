using AStar.Dev.Logging.Extensions.Models;
using AStar.Dev.Utilities;

namespace AStar.Dev.Logging.Extensions.Tests.Unit.Models;

[TestSubject(typeof(Extensions.Models.Serilog))]
public class SerilogShould
{
    [Fact]
    public void SetTheEnrichPropertyToAnEmptyArrayByDefault()
    {
        var serilog = new Extensions.Models.Serilog();

        serilog.Enrich.ShouldNotBeNull();
        serilog.Enrich.ShouldBeEmpty();
    }

    [Fact]
    public void SetTheEnrichPropertyToTheProvidedValues()
    {
        var serilog       = new Extensions.Models.Serilog();
        var testEnrichers = new[] { "Enricher1", "Enricher2" };

        serilog.Enrich = testEnrichers;

        serilog.Enrich.ShouldBe(testEnrichers);
    }

    [Fact]
    public void SetTheWriteToPropertyToAnEmptyArrayByDefault()
    {
        var serilog = new Extensions.Models.Serilog();

        var writeTo = serilog.WriteTo;

        writeTo.ToJson().ShouldMatchApproved();
    }

    [Fact]
    public void SetTheWriteToPropertyToTheProvidedValues()
    {
        var serilog = new Extensions.Models.Serilog();

        var writeToConfigs = new[] { new WriteTo { Name = "Console", Args = new() { ServerUrl = "http://localhost" } }, new WriteTo { Name = "File", Args = new() { ServerUrl = "C:\\Logs" } } };

        serilog.WriteTo = writeToConfigs;

        serilog.WriteTo.ShouldBe(writeToConfigs);
    }

    [Fact]
    public void SetTheMinimumLevelPropertyToAnEmptyInstanceByDefault()
    {
        var serilog = new Extensions.Models.Serilog();

        serilog.MinimumLevel.ShouldNotBeNull();
        serilog.MinimumLevel.Default.ShouldBe(string.Empty);
        serilog.MinimumLevel.Override.ShouldNotBeNull();
    }

    [Fact]
    public void SetTheMinimumLevelPropertyToTheProvidedValues()
    {
        var serilog              = new Extensions.Models.Serilog();
        var modifiedMinimumLevel = new MinimumLevel { Default = "Error", Override = new() { MicrosoftAspNetCore = "Warning", SystemNetHttp = "Information", AStar = "Debug" } };

        serilog.MinimumLevel = modifiedMinimumLevel;

        serilog.MinimumLevel.ShouldBe(modifiedMinimumLevel);
        serilog.MinimumLevel.Default.ShouldBe("Error");
        serilog.MinimumLevel.Override.MicrosoftAspNetCore.ShouldBe("Warning");
        serilog.MinimumLevel.Override.SystemNetHttp.ShouldBe("Information");
        serilog.MinimumLevel.Override.AStar.ShouldBe("Debug");
    }
}