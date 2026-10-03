using AStarDev.WallpaperScraper.Downloads;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.Downloads;

public sealed class GivenADirectoryEmptier
{
    private readonly MockFileSystem fileSystem = new();

    [Fact]
    public void when_the_directory_has_files_and_subdirectories_then_they_are_all_removed_and_the_directory_stays()
    {
        fileSystem.Directory.CreateDirectory("/root/top-wallpapers");
        fileSystem.File.WriteAllText("/root/top-wallpapers/one.jpg", "one");
        fileSystem.File.WriteAllText("/root/two.jpg", "two");

        fileSystem.EmptyDirectory("/root");

        (fileSystem.Directory.Exists("/root"), fileSystem.Directory.GetFileSystemEntries("/root").Length).ShouldBe((true, 0));
    }

    [Fact]
    public void when_the_directory_does_not_exist_then_it_is_not_created()
    {
        fileSystem.EmptyDirectory("/missing");

        fileSystem.Directory.Exists("/missing").ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_path_is_blank_then_nothing_is_removed(string path)
    {
        fileSystem.File.WriteAllText("/keep.jpg", "keep");

        fileSystem.EmptyDirectory(path);

        fileSystem.File.Exists("/keep.jpg").ShouldBeTrue();
    }

    [Fact]
    public void when_the_path_is_a_file_system_root_then_nothing_is_removed()
    {
        fileSystem.File.WriteAllText("/keep.jpg", "keep");

        fileSystem.EmptyDirectory("/");

        fileSystem.File.Exists("/keep.jpg").ShouldBeTrue();
    }

    [Fact]
    public void when_a_sibling_directory_exists_then_it_is_left_alone()
    {
        _ = fileSystem.Directory.CreateDirectory("/root");
        _ = fileSystem.Directory.CreateDirectory("/other");
        fileSystem.File.WriteAllText("/root/one.jpg", "one");
        fileSystem.File.WriteAllText("/other/two.jpg", "two");

        fileSystem.EmptyDirectory("/root");

        fileSystem.File.Exists("/other/two.jpg").ShouldBeTrue();
    }
}
