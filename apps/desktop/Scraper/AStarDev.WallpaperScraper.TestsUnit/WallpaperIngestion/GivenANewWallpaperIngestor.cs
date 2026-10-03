using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Scraping;
using AStarDev.WallpaperScraper.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using AStarDev.WallpaperScraper.WallpaperIngestion;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.WallpaperIngestion;

/// <summary>Runs the real downloader, file recorder, tags processor and notifier behind the ingestor, faking only the network, file system and stores, and asserts on the files written, rows stored, tags linked and messages reported.</summary>
public sealed class GivenANewWallpaperIngestor
{
    private static readonly byte[] ImageBytes = [1, 2, 3, 4, 5];
    private static readonly string[] PersonCategories = ["Celebrities"];

    private readonly MockFileSystem fileSystem = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly FakeRepository<TagEntity, TagId> tagRepository;
    private readonly FakeFileTagRepository fileTagRepository = new();
    private readonly FakeTagsQuery tagsQuery = new();
    private readonly FakeIgnoredWallpapers ignoredWallpapers = new();
    private readonly TagFlagStore flagStore = new();
    private readonly ImageDownloadNotifier notifier = new();
    private readonly List<WallpaperDownloadDetails> notifications = [];
    private readonly CapturingProgress progress = new();
    private readonly WallhavenStub wallhaven = new();
    private readonly NewWallpaperIngestor ingestor;

    public GivenANewWallpaperIngestor()
    {
        fileRepository.Store = file =>
        {
            file.Id = FileId.Create();

            return file;
        };
        tagRepository = unitOfWork.Register<TagEntity, TagId>();
        tagRepository.Store = tag =>
        {
            tag.Id = TagId.Create();

            return tag;
        };
        notifier.ImageDownloaded += (_, details) => notifications.Add(details);
        ingestor = new(
            new TagFetcher(new JsonResponseProcessor(), tagsQuery, flagStore),
            new WallpaperFiler(new WallpaperSaver(new ImageDownloader(fileSystem, System.TimeProvider.System, DownloadPacing.None, ScrapeTimeouts.Default), new WallpaperFileRecorder(System.TimeProvider.System)), new TagLinker(tagsQuery, unitOfWork, fileTagRepository, flagStore), notifier),
            new IgnoredWallpaperRecorder(ignoredWallpapers));
    }

    [Fact]
    public async Task when_a_wallpaper_is_ingested_then_its_image_is_saved_recorded_and_its_tags_are_linked()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];

        await Ingest(CreateWallpaper("new-wallpaper"), ".jpg", directory: "resolved-directory");

        var savedPath = fileSystem.Path.Combine("resolved-directory", "new-wallpaper.jpg");
        (await fileSystem.File.ReadAllBytesAsync(savedPath, TestContext.Current.CancellationToken)).ShouldBe(ImageBytes);
        var recorded = fileRepository.Added.Single();
        (recorded.FileName, recorded.DirectoryName, recorded.FileHandle).ShouldBe((new FileName("new-wallpaper.jpg"), DirectoryName.Create("resolved-directory"), FileHandle.Create("new-wallpaper")));
        var link = fileTagRepository.Added.Single();
        (link.FileId, link.TagId).ShouldBe((recorded.Id, tagRepository.Added.Single().Id));
        progress.Messages.ShouldContain("No existing data found for wallpaper new-wallpaper.");
        progress.Messages.ShouldContain("Downloaded image data for wallpaper new-wallpaper");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed"));
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_tag_flagged_to_ignore_images_then_it_is_not_downloaded_recorded_or_linked()
    {
        wallhaven.Tags = [WallhavenTag(1, "kept"), WallhavenTag(2, "unwanted")];
        tagsQuery.Tags.Add(new TagEntity { Id = TagId.Create(), WallhavenTagId = 2, Name = "unwanted", IgnoreImage = true });

        await Ingest(CreateWallpaper("ignored-wallpaper"), ".jpg");

        progress.Messages.ShouldContain("Ignoring wallpaper ignored-wallpaper: it has a tag flagged to ignore images.");
        (fileSystem.Directory.Exists("some-directory"), fileRepository.Added.Count, fileTagRepository.Added.Count, notifications.Count).ShouldBe((false, 0, 0, 0));
    }

    [Fact]
    public async Task when_a_wallpaper_is_ignored_then_its_handle_is_recorded_so_later_scrapes_skip_it()
    {
        wallhaven.Tags = [WallhavenTag(2, "unwanted")];
        tagsQuery.Tags.Add(new TagEntity { Id = TagId.Create(), WallhavenTagId = 2, Name = "unwanted", IgnoreImage = true });

        await Ingest(CreateWallpaper("ignored-wallpaper"), ".jpg");

        ignoredWallpapers.Recorded.ShouldBe([FileHandle.Create("ignored-wallpaper")]);
    }

    [Fact]
    public async Task when_a_wallpaper_is_not_ignored_then_no_handle_is_recorded_as_ignored()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];

        await Ingest(CreateWallpaper("kept-wallpaper"), ".jpg");

        ignoredWallpapers.Recorded.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_recording_the_ignored_handle_fails_then_the_failure_is_reported_and_the_wallpaper_is_still_ignored()
    {
        wallhaven.Tags = [WallhavenTag(2, "unwanted")];
        tagsQuery.Tags.Add(new TagEntity { Id = TagId.Create(), WallhavenTagId = 2, Name = "unwanted", IgnoreImage = true });
        ignoredWallpapers.RecordResult = new InvalidOperationException("record failed");

        await Ingest(CreateWallpaper("ignored-wallpaper"), ".jpg");

        progress.Messages.ShouldContain("Failed to remember that wallpaper ignored-wallpaper is ignored: record failed");
        (fileSystem.Directory.Exists("some-directory"), fileRepository.Added.Count).ShouldBe((false, 0));
    }

    [Fact]
    public async Task when_a_wallpaper_is_downloaded_then_the_download_is_announced_with_the_saved_path_and_details()
    {
        var wallpaper = new Data("wallpaper-3", 1920, 1080, 5, "image/jpeg", "https://example.test/image.jpg");

        await Ingest(wallpaper, ".jpg");

        notifications.ShouldBe([new WallpaperDownloadDetails(fileSystem.Path.Combine("some-directory", "wallpaper-3.jpg"), new WallpaperInfo("wallpaper-3.jpg", "resolved-category", 5, 1920, 1080, new SearchCount(1, 0)))]);
    }

    [Fact]
    public async Task when_only_the_tags_are_fetched_then_nothing_is_downloaded_recorded_or_linked()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];
        using var client = wallhaven.CreateClient();

        var tags = await ingestor.FetchTagsAsync(CreateWallpaper("tags-only"), IngestionRuns.Create(CreateContext("some-directory", client), progress, TestContext.Current.CancellationToken));

        tags.TryGetValue(out var fetched).ShouldBeTrue();
        fetched.Select(tag => tag.Name).ShouldBe(["landscape"]);
        (fileSystem.Directory.Exists("some-directory"), fileRepository.Added.Count, fileTagRepository.Added.Count, progress.Messages.Contains("No existing data found for wallpaper tags-only.")).ShouldBe((false, 0, 0, true));
    }

    [Fact]
    public async Task when_downloading_fails_then_the_failure_is_reported_and_nothing_is_saved_recorded_linked_or_announced()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];
        wallhaven.ImageStatus = HttpStatusCode.InternalServerError;

        await Ingest(CreateWallpaper("failing-download"), ".jpg");

        progress.Messages.ShouldContain(message => message.StartsWith("Failed to process image for wallpaper failing-download: ", StringComparison.Ordinal));
        (fileSystem.Directory.Exists("some-directory") && fileSystem.Directory.GetFiles("some-directory").Length > 0, fileRepository.Added.Count, fileTagRepository.Added.Count, notifications.Count).ShouldBe((false, 0, 0, 0));
    }

    [Fact]
    public async Task when_recording_the_file_fails_then_the_failure_is_reported_and_no_tags_are_linked()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];
        fileRepository.AddFailure = Option.Some<Exception>(new InvalidOperationException("process failed"));

        await Ingest(CreateWallpaper("failing-process"), ".jpg");

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-process: process failed");
        fileTagRepository.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_fetching_the_tags_fails_then_it_is_reported_distinctly_and_the_wallpaper_is_left_to_be_retried_untouched()
    {
        wallhaven.DetailStatus = HttpStatusCode.InternalServerError;

        await Ingest(CreateWallpaper("failing-tags"), ".jpg");

        progress.Messages.ShouldContain(message => message.StartsWith("Failed to fetch tags for wallpaper failing-tags: ", StringComparison.Ordinal));
        progress.Messages.ShouldNotContain(message => message.Contains("Downloaded image data"));
        (fileSystem.Directory.Exists("some-directory"), fileRepository.Added.Count, fileTagRepository.Added.Count, notifications.Count).ShouldBe((false, 0, 0, 0));
    }

    [Fact]
    public async Task when_linking_the_tags_fails_then_it_is_reported_distinctly_and_not_thrown()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];
        fileTagRepository.Failure = Option.Some<Exception>(new InvalidOperationException("link failed"));

        await Ingest(CreateWallpaper("failing-link"), ".jpg");

        progress.Messages.ShouldContain("Failed to link tags for wallpaper failing-link: link failed");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed to process image"));
        fileRepository.Deleted.ShouldBe(fileRepository.Added);
    }

    [Fact]
    public async Task when_linking_the_tags_fails_then_the_download_is_not_announced()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];
        fileTagRepository.Failure = Option.Some<Exception>(new InvalidOperationException("link failed"));

        await Ingest(CreateWallpaper("unannounced-link"), ".jpg");

        notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_linking_the_tags_then_the_download_is_not_announced()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];
        fileTagRepository.Failure = Option.Some<Exception>(new OperationCanceledException());

        _ = await Should.ThrowAsync<OperationCanceledException>(() => Ingest(CreateWallpaper("unannounced-cancel"), ".jpg"));

        notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_wallpaper_is_ingested_then_its_tags_are_already_linked_when_the_download_is_announced()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];
        var linksWhenAnnounced = -1;
        notifier.ImageDownloaded += (_, _) => linksWhenAnnounced = fileTagRepository.Added.Count;

        await Ingest(CreateWallpaper("announced-after-link"), ".jpg");

        linksWhenAnnounced.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_linking_the_tags_then_the_recorded_file_is_discarded_and_the_cancellation_propagates()
    {
        wallhaven.Tags = [WallhavenTag(1, "landscape")];
        fileTagRepository.Failure = Option.Some<Exception>(new OperationCanceledException());

        _ = await Should.ThrowAsync<OperationCanceledException>(() => Ingest(CreateWallpaper("cancelled-link"), ".jpg"));

        fileRepository.Deleted.ShouldBe(fileRepository.Added);
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_downloading_then_the_cancellation_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        wallhaven.OnImageRequested = () =>
        {
            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        };

        _ = await Should.ThrowAsync<OperationCanceledException>(() => IngestWithToken(CreateWallpaper("cancelled"), ".jpg", "some-directory", cancellationTokenSource.Token));

        progress.Messages.ShouldNotContain(message => message.Contains("Failed"));
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_after_downloading_then_nothing_is_recorded_or_announced_and_the_cancellation_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        wallhaven.OnImageRequested = cancellationTokenSource.Cancel;

        _ = await Should.ThrowAsync<OperationCanceledException>(() => IngestWithToken(CreateWallpaper("cancelled-after"), ".jpg", "some-directory", cancellationTokenSource.Token));

        (notifications.Count, fileRepository.Added.Count).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_famous_tag_then_it_is_saved_under_the_famous_directory()
    {
        wallhaven.Tags = [WallhavenTag(1, "Emma Watson", category: "Celebrities")];

        await Ingest(CreateWallpaper("famous-wallpaper"), ".jpg", directory: "resolved-directory");

        (fileSystem.File.Exists(fileSystem.Path.Combine("famous-resolved-directory", "emma-watson", "Emma_Watson_famous-wallpaper.jpg")), fileSystem.Directory.Exists("resolved-directory")).ShouldBe((true, false));
    }

    private Task Ingest(Data wallpaper, string extension, string directory = "some-directory")
        => IngestWithToken(wallpaper, extension, directory, TestContext.Current.CancellationToken);

    private async Task IngestWithToken(Data wallpaper, string extension, string directory, CancellationToken cancellationToken)
    {
        using var client = wallhaven.CreateClient();
        var run = IngestionRuns.Create(CreateContext(directory, client), progress, cancellationToken);

        var tags = await ingestor.FetchTagsAsync(wallpaper, run);
        if (tags.TryGetValue(out var fetched)) await ingestor.IngestAsync(new WallpaperCandidate(wallpaper, extension), fetched, run);
    }

    private WallpaperIngestionContext CreateContext(string directory, HttpClient client)
        => new(new SearchOutput(new SaveDirectories(directory, $"famous-{directory}", ""), "resolved-category"), client, fileRepository, PersonCategories);

    private static Data CreateWallpaper(string id) => new(id, 0, 0, 0, "", "https://example.test/image.jpg");

    private static object WallhavenTag(int id, string name, string category = "Nature")
        => new { id, name, alias = name, category_id = 1, category, purity = "sfw" };

    private sealed class FakeFileTagRepository : IFileTagRepository
    {
        public List<FileTagEntity> Added { get; } = [];

        public Option<Exception> Failure { get; set; } = Option.None<Exception>();

        public Exceptional<FileTagEntity> Add(FileTagEntity fileTag)
        {
            if (Failure is Option<Exception>.Some failure) return failure.Value;

            Added.Add(fileTag);

            return fileTag;
        }

        public Exceptional<Unit> Delete(FileTagEntity fileTag)
        {
            _ = Added.Remove(fileTag);

            return Unit.Instance;
        }
    }

    private sealed class WallhavenStub
    {
        public IReadOnlyList<object> Tags { get; set; } = [];

        public HttpStatusCode DetailStatus { get; set; } = HttpStatusCode.OK;

        public HttpStatusCode ImageStatus { get; set; } = HttpStatusCode.OK;

        public Action OnImageRequested { get; set; } = () => { };

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
        public HttpClient CreateClient() => new(new StubHttpMessageHandler(Respond)) { BaseAddress = new Uri("https://example.test/") };

        private HttpResponseMessage Respond(HttpRequestMessage request)
        {
            if (request.RequestUri!.AbsolutePath.StartsWith($"/{ApplicationConstants.WallhavenDetailPathTemplate}", StringComparison.Ordinal))
                return new HttpResponseMessage(DetailStatus) { Content = new StringContent(JsonSerializer.Serialize(new { data = new { tags = Tags } }), System.Text.Encoding.UTF8, "application/json") };

            OnImageRequested();

            return new HttpResponseMessage(ImageStatus) { Content = new ByteArrayContent(ImageBytes) };
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
