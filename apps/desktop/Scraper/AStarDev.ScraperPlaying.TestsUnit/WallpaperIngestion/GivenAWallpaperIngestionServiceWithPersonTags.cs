using System.Diagnostics.CodeAnalysis;
using System.Net;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions.Testing;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperIngestionServiceWithPersonTags
{
    private readonly MockFileSystem fileSystem = new();
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly FakeFilesQuery filesQuery = new();
    private readonly StubTagsProcessor tagsProcessor = new();
    private readonly WallpaperIngestionService service;

    public GivenAWallpaperIngestionServiceWithPersonTags()
    {
        var newWallpaperIngestor = new NewWallpaperIngestor(tagsProcessor, new WallpaperFiler(new WallpaperSaver(new ImageDownloader(fileSystem, System.TimeProvider.System, DownloadPacing.None, ScrapeTimeouts.Default), new WallpaperFileRecorder(new FakeTimeProvider(DateTimeOffset.UnixEpoch))), tagsProcessor, new ImageDownloadNotifier()), new IgnoredWallpaperRecorder(new FakeIgnoredWallpapers()));
        service = new(filesQuery, newWallpaperIngestor);
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_famous_tag_then_it_is_saved_under_the_famous_directory_and_prefixed_with_the_name()
    {
        tagsProcessor.Tags = [new Tag(1, "finger pointing", "finger-pointing", 51, "Other Figures", "sfw"), new Tag(2, "Max Verstappen", "max-verstappen", 51, "Other Figures", "sfw", IsFamous: true)];

        await Ingest("abc123");

        fileSystem.File.Exists(fileSystem.Path.Combine("famous-some-directory", "max-verstappen", "", "Max_Verstappen_abc123.jpg")).ShouldBeTrue();
        fileSystem.File.Exists(fileSystem.Path.Combine("famous-some-directory", "max-verstappen", "", "abc123.jpg")).ShouldBeFalse();
        fileRepository.Added.Select(file => file.FileName.Value).ShouldBe(["Max_Verstappen_abc123.jpg"]);
        fileRepository.Added.Select(file => file.DirectoryName.Value).ShouldBe([Path.Combine("famous-some-directory", "max-verstappen", "")]);
        fileRepository.Added.Select(file => file.FileHandle.Value).ShouldBe(["abc123"]);
    }

    [Fact]
    public async Task when_a_wallpaper_has_no_person_name_tag_then_the_wallpaper_id_is_the_file_name()
    {
        tagsProcessor.Tags = [new Tag(1, "landscape", "landscape", 5, "Nature", "sfw")];

        await Ingest("abc123");

        fileSystem.File.Exists(fileSystem.Path.Combine("some-directory", "abc123.jpg")).ShouldBeTrue();
        fileRepository.Added.Select(file => file.FileName.Value).ShouldBe(["abc123.jpg"]);
    }

    [Fact]
    public async Task when_fetching_the_tags_fails_then_the_image_is_not_saved_so_the_wallpaper_is_retried_on_the_next_scrape()
    {
        tagsProcessor.FetchFailure = new HttpRequestException("tag fetch failed");

        await Ingest("abc123");

        fileSystem.File.Exists(fileSystem.Path.Combine("some-directory", "abc123.jpg")).ShouldBeFalse();
        fileRepository.Added.ShouldBeEmpty();
        tagsProcessor.LinkedTags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_wallpaper_is_saved_then_the_fetched_tags_are_linked()
    {
        tagsProcessor.Tags = [new Tag(2, "Max Verstappen", "max-verstappen", 51, "Other Figures", "sfw")];

        await Ingest("abc123");

        tagsProcessor.LinkedTags.ShouldBe(tagsProcessor.Tags);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
    private async Task Ingest(string id)
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) }));

        await service.IngestPageAsync([new Data(id, 1920, 1080, 3, "image/jpeg", $"https://example.test/full/{id}.jpg")], IngestionRuns.Create(new WallpaperIngestionContext(new SearchOutput(new SaveDirectories("some-directory", "famous-some-directory", ""), "Top Wallpapers"), client, fileRepository, ["Celebrities", "Models", "Pornstars", "Other Figures", "Actress"]), new Progress<string>()));
    }

    private sealed class StubTagsProcessor : ITagFetcher, ITagLinker
    {
        public IReadOnlyList<Tag> Tags { get; set; } = [];

        public Exception? FetchFailure { get; set; }

        public IReadOnlyList<Tag> LinkedTags { get; private set; } = [];

        public Task<Exceptional<IReadOnlyList<Tag>>> FetchTagsAsync(string wallpaperId, IngestionRun run)
            => Task.FromResult(FetchFailure is null ? Exceptional.Success(Tags) : Exceptional.Failure<IReadOnlyList<Tag>>(FetchFailure));

        public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
        {
            LinkedTags = tags;

            return Task.FromResult((Exceptional<Unit>)Unit.Instance);
        }
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
