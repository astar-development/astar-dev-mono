using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsUnit.FileDetail;

public partial class GivenAFileAccessDetailId
{
    [Fact]
    public void when_the_empty_property_is_accessed_then_the_value_is_an_empty_guid()
        => FileAccessDetailId.Empty.Value.ShouldBe(Guid.Empty);

    [Fact]
    public void when_the_create_method_is_called_then_the_value_is_not_empty()
        => FileAccessDetailId.Create().Value.ShouldNotBe(Guid.Empty);
}
