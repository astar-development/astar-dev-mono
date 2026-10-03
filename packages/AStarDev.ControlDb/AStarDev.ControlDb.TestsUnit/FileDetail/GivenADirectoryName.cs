using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsUnit.FileDetail;

public partial class GivenADirectoryName
{
    [Fact]
    public void when_create_is_called_then_the_value_is_set_correctly()
        => DirectoryName.Create("directory-name").Value.ShouldBe("directory-name");
}
