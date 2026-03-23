namespace AStar.Dev.Logging.Extensions.Tests.Unit;

[TestSubject(typeof(Configuration))]
public class ConfigurationShould
{
    [Fact]
    public void ReturnDefaultFilename_ForExternalSettingsFile()
    {
        var expected = "astar-logging-settings.json";

        var result = Configuration.ExternalSettingsFile;

        result.ShouldBe(expected);
    }

    [Fact]
    public void NotReturnEmptyString_ForExternalSettingsFile()
    {
        var result = Configuration.ExternalSettingsFile;

        result.ShouldNotBeNullOrEmpty("ExternalSettingsFile should not be an empty string.");
    }

    [Fact]
    public void NotReturnNull_ForExternalSettingsFile()
    {
        var result = Configuration.ExternalSettingsFile;

        result.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("astar-logging-settings.json")]
    [InlineData("ASTAR-LOGGING-SETTINGS.JSON")] // Case-insensitivity check
    public void MatchContentRegardlessOfCase_ForExternalSettingsFile(string comparisonValue)
    {
        var result = Configuration.ExternalSettingsFile;

        string.Equals(result, comparisonValue, StringComparison.OrdinalIgnoreCase)
              .ShouldBeTrue($"Expected {comparisonValue}, but got {result}.");
    }
}