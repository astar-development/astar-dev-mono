using AStarDev.Utilities;
using AStarDev.ControlDb.TestsUnit.TestDataFactories;
using static AStarDev.ControlDb.TestsUnit.Utilities.RegexHelpers;

namespace AStarDev.ControlDb.TestsUnit.ScrapeConfiguration;

public partial class GivenAScrapeConfigurationEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
    {
        string sut = ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity().ToJson() + Environment.NewLine;
        sut.ShouldMatchApproved(c => c.WithScrubber(s => DateTimeFormatRegex().Replace(s, "<date>")));
    }
}
