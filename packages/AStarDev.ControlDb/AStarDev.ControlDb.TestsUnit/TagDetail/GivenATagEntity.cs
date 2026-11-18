using AStarDev.ControlDb.TestsUnit.TestDataFactories;
using AStarDev.Utilities;
using static AStarDev.ControlDb.TestsUnit.Utilities.RegexHelpers;

namespace AStarDev.ControlDb.TestsUnit.TagDetail;

public partial class GivenATagEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
        => (TagEntityFactory.CreateTagEntity().ToJson() + Environment.NewLine).ShouldMatchApproved(c => c.WithScrubber(s => DateTimeFormatRegex().Replace(s, "<date>")));
}
