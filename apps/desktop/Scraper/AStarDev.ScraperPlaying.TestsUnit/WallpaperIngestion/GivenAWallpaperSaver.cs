using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperSaver
{
    private readonly StubImageDownloader downloader = new();
    private readonly StubFileRecorder recorder = new();
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperSaver saver;

    public GivenAWallpaperSaver()
    {
        saver = new(downloader, recorder);
    }

    [Fact]
    public async Task when_a_wallpaper_is_saved_then_the_recorded_entity_is_returned_with_the_details_to_announce()
    {
        var recorded = CreateEntity();
        recorder.Result = recorded;
        downloader.SavedPath = "saved/path.jpg";

        var saved = await Save(CreateRequest("wallpaper-1"), CancellationToken.None);

        saved.Entity.ShouldBeSameAs(recorded);
        saved.Details.ShouldBe(new WallpaperDownloadDetails("saved/path.jpg", new WallpaperInfo("wallpaper-1.jpg", "category", 5, 1920, 1080)));
        progress.Messages.ShouldContain("Downloaded image data for wallpaper wallpaper-1");
    }

    [Fact]
    public async Task when_the_download_fails_then_the_failure_is_thrown_and_nothing_is_recorded()
    {
        downloader.Failure = new InvalidOperationException("download failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => Save(CreateRequest("wallpaper-1"), CancellationToken.None));

        (thrown.Message, recorder.Recorded).ShouldBe(("download failed", 0));
    }

    [Fact]
    public async Task when_recording_fails_then_the_failure_is_thrown()
    {
        recorder.Result = new InvalidOperationException("record failed");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => Save(CreateRequest("wallpaper-1"), CancellationToken.None));

        thrown.Message.ShouldBe("record failed");
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_after_the_download_then_nothing_is_recorded_and_the_cancellation_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        downloader.OnDownloaded = cancellationTokenSource.Cancel;

        _ = await Should.ThrowAsync<OperationCanceledException>(() => Save(CreateRequest("wallpaper-1"), cancellationTokenSource.Token));

        recorder.Recorded.ShouldBe(0);
    }

    private async Task<SavedWallpaper> Save(WallpaperFileRequest request, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();

        return await saver.SaveAsync(request, new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("some-directory", "famous-some-directory", ""), "category"), client, fileRepository, []), progress, cancellationToken);
    }

    private static FileEntity CreateEntity()
        => new WallpaperFileRecorder(System.TimeProvider.System).Record(new FakeRepository<FileEntity, FileId>(), CreateRequest("recorded")).Match(entity => entity, exception => throw exception);

    private static WallpaperFileRequest CreateRequest(string id)
    {
        var wallpaper = new Data(id, 1920, 1080, 5, "image/jpeg", $"https://example.test/{id}.jpg");

        return new WallpaperFileRequest(wallpaper, "some-directory", WallpaperFileNamer.Create(id, ".jpg", []), "category");
    }

    private sealed class StubImageDownloader : IImageDownloader
    {
        public string SavedPath { get; set; } = "saved.jpg";

        public Exception? Failure { get; set; }

        public Action OnDownloaded { get; set; } = () => { };

        public Task<string> DownloadAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
        {
            if (Failure is not null) throw Failure;

            OnDownloaded();

            return Task.FromResult(SavedPath);
        }
    }

    private sealed class StubFileRecorder : IWallpaperFileRecorder
    {
        public Exceptional<FileEntity> Result { get; set; } = CreateEntity();

        public int Recorded { get; private set; }

        public Exceptional<FileEntity> Record(IRepository<FileEntity, FileId> fileRepository, WallpaperFileRequest request)
        {
            Recorded++;

            return Result;
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
