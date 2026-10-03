using AStarDev.ControlDb.TagDetail;

namespace AStarDev.ControlDb.TestsUnit.TagDetail;

public partial class GivenATagId
{
    [Fact]
    public void when_the_empty_property_is_accessed_then_the_value_is_an_empty_guid()
        => TagId.Empty.Value.ShouldBe(Guid.Empty);

    [Fact]
    public void when_the_create_method_is_called_then_the_value_is_not_empty()
        => TagId.Create().Value.ShouldNotBe(Guid.Empty);
}
