using AStarDev.Utilities;
using AStarDev.ControlDb.TestsUnit.TestDataFactories;
using static AStarDev.ControlDb.TestsUnit.Utilities.RegexHelpers;

namespace AStarDev.ControlDb.TestsUnit.TagDetail;

public partial class GivenAFileTagEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
        => (FileTagEntityFactory.CreateFileTagEntity().ToJson() + Environment.NewLine).ShouldMatchApproved(c => c.WithScrubber(s => DateTimeFormatRegex().Replace(s, "<date>")));
}
