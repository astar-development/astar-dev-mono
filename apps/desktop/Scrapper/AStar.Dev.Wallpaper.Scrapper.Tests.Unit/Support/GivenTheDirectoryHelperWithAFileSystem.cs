using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheDirectoryHelperWithAFileSystem
{
    private readonly MockFileSystem fileSystem = new();
    private readonly IDirectoryHelper sut;

    public GivenTheDirectoryHelperWithAFileSystem() => sut = new DirectoryHelper(fileSystem);

    [Fact]
    public void when_the_directory_does_not_exist_then_it_is_created()
    {
        sut.CreateDirectoryIfRequired(["root", "sub-directory",]);

        fileSystem.Directory.Exists(Path.Combine("root", "sub-directory")).ShouldBeTrue();
    }

    [Fact]
    public void when_the_directory_is_created_then_the_returned_directory_name_is_the_combined_pre_clean_path()
    {
        var directoryName = sut.CreateDirectoryIfRequired(["root", "sub-directory",]);

        directoryName.Value.ShouldBe(Path.Combine("root", "sub-directory"));
    }

    [Fact]
    public void when_the_combined_path_contains_characters_requiring_cleaning_then_the_directory_is_created_at_the_cleaned_path_but_the_returned_name_keeps_the_uncleaned_path()
    {
        var directoryName = sut.CreateDirectoryIfRequired(["root", "sub\"directory",]);

        fileSystem.Directory.Exists(Path.Combine("root", "sub'directory")).ShouldBeTrue();
        directoryName.Value.ShouldBe(Path.Combine("root", "sub\"directory"));
    }
}
