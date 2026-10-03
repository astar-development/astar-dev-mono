using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsUnit.FileDetail;

public partial class GivenAFileHandle
{
    [Fact]
    public void when_create_is_called_then_the_value_is_set_correctly()
        => FileHandle.Create("file-handle").Value.ShouldBe("file-handle");
}
