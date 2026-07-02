using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenAPathOperationExtensions
{
    [Fact]
    public void when_cleaning_a_path_then_at_signs_are_preserved()
    {
        var path = "/tmp/user@domain/photos";

        var cleanedPath = path.CleanPath();

        cleanedPath.ShouldBe("/tmp/user@domain/photos");
    }
}
