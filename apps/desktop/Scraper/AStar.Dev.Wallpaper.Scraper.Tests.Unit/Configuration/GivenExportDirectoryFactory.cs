using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Configuration;

public class GivenExportDirectoryFactory
{
    [Fact]
    public void when_creating_from_a_valid_path_then_the_value_is_preserved()
    {
        var result = ExportDirectoryFactory.Create("/exports");

        result.Match(exportDirectory => exportDirectory.Value, error => error).ShouldBe("/exports");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_creating_from_a_null_empty_or_whitespace_path_then_a_failure_is_returned(string? rawPath)
    {
        var result = ExportDirectoryFactory.Create(rawPath);

        result.Match(exportDirectory => exportDirectory.Value, error => error).ShouldBe("Export directory must not be null, empty, or whitespace.");
    }
}
