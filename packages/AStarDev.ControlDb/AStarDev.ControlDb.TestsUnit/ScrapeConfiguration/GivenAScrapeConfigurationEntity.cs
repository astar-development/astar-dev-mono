using System.Text.RegularExpressions;
using AStarDev.Utilities;
using AStarDev.ControlDb.TestsUnit.TestDataFactories;

namespace AStarDev.ControlDb.TestsUnit.ScrapeConfiguration;

public partial class GivenAScrapeConfigurationEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
    {
        string sut = ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity().ToJson() + Environment.NewLine;
        sut.ShouldMatchApproved(c => c.WithScrubber(s => DateTimeFormatRegex().Replace(s, "<date>")));
    }

    [GeneratedRegex(@"\d{1,4}-\d{1,2}-\d{1,2}T\d{1,2}:\d{1,2}:\d{1,2}\.\d{1,7}\+00:00")]
    private static partial Regex DateTimeFormatRegex();
}
