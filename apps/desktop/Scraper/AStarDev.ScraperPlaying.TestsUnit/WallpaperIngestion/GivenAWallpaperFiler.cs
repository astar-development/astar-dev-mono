using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperFiler
{
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly StubTagLinker tagLinker = new();
    private readonly ImageDownloadNotifier notifier = new();
    private readonly List<WallpaperDownloadDetails> notifications = [];
    private readonly CapturingProgress progress = new();
    private readonly WallpaperFiler filer;

    public GivenAWallpaperFiler()
    {
        fileRepository.Store = file =>
        {
            file.Id = FileId.Create();

            return file;
        };
        notifier.ImageDownloaded += (_, details) => notifications.Add(details);
        filer = new(new WallpaperSaver(new StubImageDownloader(), new WallpaperFileRecorder(System.TimeProvider.System)), tagLinker, notifier);
    }

    [Fact]
    public async Task when_the_wallpaper_is_saved_and_linked_then_it_is_complete_and_announced_with_its_tags_linked()
    {
        var recorded = default(FileId);
        tagLinker.OnLink = fileId => recorded = fileId;

        var outcome = await File("filed-wallpaper", CancellationToken.None);

        (outcome, fileRepository.Added.Single().Id).ShouldBe((IngestOutcome.Complete, recorded));
        notifications.ShouldBe([new WallpaperDownloadDetails("saved.jpg", new WallpaperInfo("filed-wallpaper.jpg", "category", 5, 1920, 1080, new SearchCount(1, 0)))]);
        fileRepository.Deleted.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_several_wallpapers_of_a_search_are_filed_then_each_is_announced_with_its_running_count_and_the_search_total()
    {
        using var client = new HttpClient();
        var context = new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("some-directory", "famous-some-directory", ""), "category"), client, fileRepository, []);
        var run = IngestionRuns.Create(context, progress, TestContext.Current.CancellationToken);
        run.Tally.Total = 1124;

        await filer.FileAsync(new WallpaperCandidate(new Data("first", 1920, 1080, 5, "image/jpeg", "https://example.test/first.jpg"), ".jpg"), [], run);
        await filer.FileAsync(new WallpaperCandidate(new Data("second", 1920, 1080, 5, "image/jpeg", "https://example.test/second.jpg"), ".jpg"), [], run);

        notifications.Select(details => details.Info.Count).ShouldBe([new SearchCount(1, 1124), new SearchCount(2, 1124)]);
    }

    [Fact]
    public async Task when_linking_fails_then_it_is_reported_the_file_is_discarded_and_nothing_is_announced()
    {
        tagLinker.Result = new InvalidOperationException("link failed");

        var outcome = await File("failing-link", CancellationToken.None);

        progress.Messages.ShouldContain("Failed to link tags for wallpaper failing-link: link failed");
        outcome.ShouldBe(IngestOutcome.Incomplete);
        fileRepository.Deleted.ShouldBe(fileRepository.Added);
        notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_discarding_the_file_fails_then_that_is_reported_and_the_outcome_is_still_incomplete()
    {
        tagLinker.Result = new InvalidOperationException("link failed");
        fileRepository.DeleteFailure = Option.Some<Exception>(new InvalidOperationException("delete failed"));

        var outcome = await File("failing-discard", CancellationToken.None);

        progress.Messages.ShouldContain("Failed to discard the untagged file record for wallpaper failing-discard: delete failed");
        outcome.ShouldBe(IngestOutcome.Incomplete);
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_linking_then_the_file_is_discarded_and_the_cancellation_propagates_without_an_announcement()
    {
        tagLinker.Failure = new OperationCanceledException();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => File("cancelled-link", CancellationToken.None));

        fileRepository.Deleted.ShouldBe(fileRepository.Added);
        notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_saving_fails_then_the_failure_is_thrown_and_nothing_is_linked_or_announced()
    {
        fileRepository.AddFailure = Option.Some<Exception>(new InvalidOperationException("record failed"));

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() => File("failing-save", CancellationToken.None));

        (thrown.Message, tagLinker.Linked).ShouldBe(("record failed", 0));
        notifications.ShouldBeEmpty();
    }

    private async Task<IngestOutcome> File(string id, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var context = new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("some-directory", "famous-some-directory", ""), "category"), client, fileRepository, []);
        var candidate = new WallpaperCandidate(new Data(id, 1920, 1080, 5, "image/jpeg", $"https://example.test/{id}.jpg"), ".jpg");

        return await filer.FileAsync(candidate, [], IngestionRuns.Create(context, progress, cancellationToken));
    }

    private sealed class StubImageDownloader : IImageDownloader
    {
        public Task<string> DownloadAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
            => Task.FromResult("saved.jpg");
    }

    private sealed class StubTagLinker : ITagLinker
    {
        public Exceptional<Unit> Result { get; set; } = Unit.Instance;

        public Exception? Failure { get; set; }

        public Action<FileId> OnLink { get; set; } = _ => { };

        public int Linked { get; private set; }

        public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
        {
            if (Failure is not null) throw Failure;

            Linked++;
            OnLink(fileId);

            return Task.FromResult(Result);
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
