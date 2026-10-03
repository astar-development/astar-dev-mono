using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using AStarDev.WallpaperScraper.WallpaperIngestion;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperFileRecorder
{
    private static readonly DateTimeOffset now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly WallpaperFileRecorder recorder = new(new FakeTimeProvider(now));

    [Fact]
    public void when_recording_a_wallpaper_then_a_matching_file_entity_is_added_and_returned()
    {
        var wallpaper = CreateWallpaper(id: "wallpaper-1", fileSize: 1234, fileType: "image/jpeg", dimensionX: 1920, dimensionY: 1080, path: "https://example.test/full/wallpaper-1.jpg");

        var result = recorder.Record(fileRepository, new WallpaperFileRequest(wallpaper, "some-directory", NameFor(wallpaper, ".jpg"), "Top Wallpapers"));

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.FileName.Value.ShouldBe("wallpaper-1.jpg");
        addedEntity.FileHandle.Value.ShouldBe("wallpaper-1");
        addedEntity.FileSize.ShouldBe(1234);
        addedEntity.FileType.ShouldBe("image/jpeg");
        addedEntity.IsImage.ShouldBeTrue();
        addedEntity.ImageDetail!.Width.ShouldBe(1920);
        addedEntity.ImageDetail!.Height.ShouldBe(1080);
        addedEntity.FileAccessDetail.DetailsLastUpdated.ShouldBe(now.UtcDateTime);
        addedEntity.LastUpdated.ShouldBe(now);
        fileRepository.Added.ShouldBe([addedEntity]);
    }

    [Fact]
    public void when_the_request_has_a_prefixed_file_name_then_it_is_used_for_the_file_entity_and_the_handle_stays_the_wallpaper_id()
    {
        var wallpaper = CreateWallpaper(id: "abc123", path: "https://example.test/full/abc123.jpg");
        var request = new WallpaperFileRequest(wallpaper, "some-directory", new FileName("Max_Verstappen_abc123.jpg"), "Top Wallpapers");

        var entity = recorder.Record(fileRepository, request).Match(added => added, ex => throw ex);

        (entity.FileName.Value, entity.FileHandle.Value).ShouldBe(("Max_Verstappen_abc123.jpg", "abc123"));
    }

    [Fact]
    public void when_a_directory_is_supplied_then_it_is_used_as_the_directory_name()
    {
        var wallpaper = CreateWallpaper(id: "wallpaper-top");

        var entity = recorder.Record(fileRepository, new WallpaperFileRequest(wallpaper, "root-directory/top-wallpapers", NameFor(wallpaper, ".jpg"), "Top Wallpapers")).Match(added => added, ex => throw ex);

        entity.DirectoryName.Value.ShouldBe("root-directory/top-wallpapers");
    }

    [Fact]
    public void when_the_wallpaper_path_does_not_have_an_image_extension_then_is_image_is_false()
    {
        var wallpaper = CreateWallpaper(id: "wallpaper-5", path: "https://example.test/full/wallpaper-5.txt");

        var entity = recorder.Record(fileRepository, new WallpaperFileRequest(wallpaper, "some-directory", NameFor(wallpaper, ".txt"), "Top Wallpapers")).Match(added => added, ex => throw ex);

        entity.IsImage.ShouldBeFalse();
    }

    [Fact]
    public void when_adding_the_file_entity_fails_then_the_failure_is_returned_not_swallowed()
    {
        var exception = new InvalidOperationException("add failed");
        fileRepository.AddFailure = Option.Some<Exception>(exception);
        var wallpaper = CreateWallpaper(id: "wallpaper-2");

        var result = recorder.Record(fileRepository, new WallpaperFileRequest(wallpaper, "some-directory", NameFor(wallpaper, ".jpg"), "Top Wallpapers"));

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
    }

    private static FileName NameFor(Data wallpaper, string extension) => new($"{wallpaper.Id}{extension}");

    private static Data CreateWallpaper(string id, int fileSize = 0, string fileType = "", int dimensionX = 0, int dimensionY = 0, string path = "")
        => new(id, dimensionX, dimensionY, fileSize, fileType, path);
}
