using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsUnit.FileDetail;

public partial class GivenAnImageId
{
    [Fact]
    public void when_the_empty_property_is_accessed_then_the_value_is_an_empty_guid()
        => ImageId.Empty.Value.ShouldBe(Guid.Empty);

    [Fact]
    public void when_the_create_method_is_called_then_the_value_is_not_empty()
        => ImageId.Create().Value.ShouldNotBe(Guid.Empty);
}
