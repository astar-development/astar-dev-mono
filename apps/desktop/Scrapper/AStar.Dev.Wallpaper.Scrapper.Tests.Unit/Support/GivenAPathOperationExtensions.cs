using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenAPathOperationExtensions
{
    [Fact]
    public void when_cleaning_a_path_then_at_signs_are_preserved()
    {
        string path = "/tmp/user@domain/photos";

        string cleanedPath = path.CleanPath();

        cleanedPath.ShouldBe("/tmp/user@domain/photos");
    }
}
