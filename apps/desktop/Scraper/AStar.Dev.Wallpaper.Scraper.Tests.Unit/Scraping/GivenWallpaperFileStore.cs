using AStar.Dev.Wallpaper.Scraper.Scraping;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenWallpaperFileStore
{
    private readonly MockFileSystem fileSystem = new();

    [Fact]
    public async Task when_the_target_directory_does_not_exist_then_it_is_created()
    {
        var sut = new WallpaperFileStore(fileSystem);

        await sut.SaveAsync("/wallpapers/nature", "pic.jpg", [1, 2, 3], TestContext.Current.CancellationToken);

        fileSystem.Directory.Exists("/wallpapers/nature").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_image_is_saved_then_its_bytes_are_written_to_the_combined_path()
    {
        byte[] imageBytes = [10, 20, 30, 40];
        var sut = new WallpaperFileStore(fileSystem);

        var saved = await sut.SaveAsync("/wallpapers/nature", "pic.jpg", imageBytes, TestContext.Current.CancellationToken);

        saved.FullPath.ShouldBe("/wallpapers/nature/pic.jpg");
        fileSystem.File.ReadAllBytes(saved.FullPath).ShouldBe(imageBytes);
    }

    [Fact]
    public async Task when_the_image_is_saved_then_the_returned_size_matches_the_written_file()
    {
        byte[] imageBytes = [1, 2, 3, 4, 5];
        var sut = new WallpaperFileStore(fileSystem);

        var saved = await sut.SaveAsync("/wallpapers/nature", "pic.jpg", imageBytes, TestContext.Current.CancellationToken);

        saved.SizeBytes.ShouldBe(imageBytes.LongLength);
    }
}
