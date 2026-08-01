using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;
using Testably.Abstractions.Testing;
using AStar.Dev.Wallpaper.Scraper.Scraping.Actions;
using AStar.Dev.Wallpaper.Scraper.Scraping.Categories;
using AStar.Dev.Wallpaper.Scraper.Scraping.Context;
using AStar.Dev.Wallpaper.Scraper.Scraping.ImageDownload;
using AStar.Dev.Wallpaper.Scraper.Scraping.Navigation;
using AStar.Dev.Wallpaper.Scraper.Scraping.Storage;
using AStar.Dev.Wallpaper.Scraper.Scraping.Tags;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping.Navigation;

public sealed class GivenWallpaperPageVisitor
{
    private readonly MockFileSystem fileSystem = new();
    private readonly ITagReader tagReader = Substitute.For<ITagReader>();
    private readonly IWallpaperImageLocator imageLocator = Substitute.For<IWallpaperImageLocator>();
    private readonly IWallpaperImageDownloader imageDownloader = Substitute.For<IWallpaperImageDownloader>();
    private readonly IImageDimensionsReader dimensionsReader = Substitute.For<IImageDimensionsReader>();
    private readonly IWallpaperFileStore fileStore = Substitute.For<IWallpaperFileStore>();
    private readonly IWallpaperCategoryRegistrar categoryRegistrar = Substitute.For<IWallpaperCategoryRegistrar>();
    private readonly IWallpaperFileClassificationRepository fileClassificationRepository = Substitute.For<IWallpaperFileClassificationRepository>();
    private readonly IProgress<string> progress = Substitute.For<IProgress<string>>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly Clock clock = () => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private const string WallpaperHref = "https://wallhaven.cc/w/abc123";
    private const string WallpaperImageUrl = "https://wallhaven.cc/images/pic.jpg";

    private static readonly byte[] imageBytes = [1, 2, 3];
    private static readonly string[] expectedNatureTagOnly = ["Nature"];
    private static readonly ScrapeCategory natureCategory = new("Nature", "https://wallhaven.cc/search?categories=1", false, false);

    private static readonly ScrapeContext singleCategoryContext = new(
        [natureCategory],
        [],
        [],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            SearchStringPrefix = "https://wallhaven.cc/search?categories=",
            SearchStringSuffix = string.Empty,
            ImagePauseInSeconds = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    private static readonly ScrapeContext ignoredTagContext = new(
        [natureCategory],
        [],
        ["Ignored"],
        new DirectoryLayout("/root", "/base", "/famous"), [], new SearchConfigurationEntity
        {
            Id = 1,
            SearchStringPrefix = "https://wallhaven.cc/search?categories=",
            SearchStringSuffix = string.Empty,
            ImagePauseInSeconds = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

    [Fact]
    public async Task when_the_wallpaper_page_fails_to_load_then_progress_reports_the_failure_and_no_tags_are_read()
    {
        var sut = CreateSut(wallpaperPageOk: false, wallpaperPageStatus: 404);
        var context = new CategoryScrapeContext(page, progress, singleCategoryContext, natureCategory, singleCategoryContext.FileClassifications);

        await sut.VisitAsync(context, WallpaperHref, TestContext.Current.CancellationToken);

        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to load wallpaper page")));
        await tagReader.DidNotReceive().ReadAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_has_no_image_url_then_progress_reports_the_failure_and_nothing_is_downloaded()
    {
        var sut = CreateSut(imageUrl: Option<string>.None.Instance);
        var context = new CategoryScrapeContext(page, progress, singleCategoryContext, natureCategory, singleCategoryContext.FileClassifications);

        await sut.VisitAsync(context, WallpaperHref, TestContext.Current.CancellationToken);

        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to get wallpaper image URL")));
        await imageDownloader.DidNotReceive().DownloadAsync(Arg.Any<IPage>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_was_already_downloaded_then_its_page_is_never_visited_and_it_is_not_downloaded_again()
    {
        var sut = CreateSut(isAlreadyDownloaded: true);
        var context = new CategoryScrapeContext(page, progress, singleCategoryContext, natureCategory, singleCategoryContext.FileClassifications);

        await sut.VisitAsync(context, WallpaperHref, TestContext.Current.CancellationToken);

        await page.DidNotReceive().GotoAsync(WallpaperHref, Arg.Any<PageGotoOptions>());
        await tagReader.DidNotReceive().ReadAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
        await imageDownloader.DidNotReceive().DownloadAsync(Arg.Any<IPage>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        await fileStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await fileClassificationRepository.DidNotReceive().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ImageDimensions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_checking_whether_a_wallpaper_is_already_downloaded_then_the_check_is_a_contains_match_on_the_wallpaper_id()
    {
        var sut = CreateSut(imageUrl: Option<string>.None.Instance);
        var context = new CategoryScrapeContext(page, progress, singleCategoryContext, natureCategory, singleCategoryContext.FileClassifications);

        await sut.VisitAsync(context, WallpaperHref, TestContext.Current.CancellationToken);

        await fileClassificationRepository.Received().IsAlreadyDownloadedAsync("abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_tag_on_the_ignore_list_then_it_is_saved_under_the_curated_directory_path()
    {
        var sut = CreateSut(tags: [new TagData("Nature", "outdoors"), new TagData("Ignored", "outdoors")]);
        var context = new CategoryScrapeContext(page, progress, ignoredTagContext, natureCategory, ignoredTagContext.FileClassifications);

        await sut.VisitAsync(context, WallpaperHref, TestContext.Current.CancellationToken);

        await fileStore.Received().SaveAsync("/root/base/N/Nature", "pic.jpg", imageBytes, Arg.Any<CancellationToken>());
        await fileStore.DidNotReceive().SaveAsync("/root/base/N/Nature/Ignored", Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_new_wallpaper_is_visited_then_its_categories_are_registered_and_it_is_downloaded_saved_and_recorded()
    {
        var dimensions = new ImageDimensions(10, 20);
        var sut = CreateSut(dimensions: dimensions);
        var context = new CategoryScrapeContext(page, progress, singleCategoryContext, natureCategory, singleCategoryContext.FileClassifications);

        await sut.VisitAsync(context, WallpaperHref, TestContext.Current.CancellationToken);

        await categoryRegistrar.Received().EnsureCategoriesExistAsync(Arg.Is<IReadOnlyList<TagData>>(tags => tags != null && tags.Any(tag => tag.Tag == "Nature")), Arg.Any<CancellationToken>());
        await fileClassificationRepository.Received().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), WallpaperImageUrl, Arg.Any<string>(), 3, dimensions, Arg.Any<CancellationToken>());
        await imageDownloader.Received().DownloadAsync(page, WallpaperImageUrl, "Nature", Arg.Is<IReadOnlyList<string>>(tags => tags!.SequenceEqual(expectedNatureTagOnly)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_download_fails_then_progress_reports_the_failure_and_nothing_is_saved_or_recorded()
    {
        var exception = new InvalidOperationException("Navigating to 'https://wallhaven.cc/images/pic.jpg' did not produce a response.");
        var sut = CreateSut(downloadResult: Exceptional.Failure<byte[]>(exception));
        var context = new CategoryScrapeContext(page, progress, singleCategoryContext, natureCategory, singleCategoryContext.FileClassifications);

        await sut.VisitAsync(context, WallpaperHref, TestContext.Current.CancellationToken);

        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to download wallpaper image")));
        await fileStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await fileClassificationRepository.DidNotReceive().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ImageDimensions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_saving_the_downloaded_file_throws_then_progress_reports_the_real_exception_detail_and_execution_still_succeeds()
    {
        var sut = CreateSut(saveException: new InvalidOperationException("disk full"));
        var context = new CategoryScrapeContext(page, progress, singleCategoryContext, natureCategory, singleCategoryContext.FileClassifications);

        await sut.VisitAsync(context, WallpaperHref, TestContext.Current.CancellationToken);

        progress.Received().Report(Arg.Is<string>(message => message!.Contains("disk full")));
        await fileClassificationRepository.DidNotReceive().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ImageDimensions>(), Arg.Any<CancellationToken>());
    }

    private WallpaperPageVisitor CreateSut(
        bool wallpaperPageOk = true,
        int wallpaperPageStatus = 200,
        IReadOnlyList<TagData>? tags = null,
        Option<string>? imageUrl = null,
        bool isAlreadyDownloaded = false,
        Exceptional<byte[]>? downloadResult = null,
        SavedWallpaperFile? savedFile = null,
        ImageDimensions? dimensions = null,
        Exception? saveException = null)
    {
        var wallpaperPageResponse = Substitute.For<IResponse>();
        wallpaperPageResponse.Ok.Returns(wallpaperPageOk);
        wallpaperPageResponse.Status.Returns(wallpaperPageStatus);
        page.GotoAsync(WallpaperHref, Arg.Any<PageGotoOptions>()).Returns(wallpaperPageResponse);

        tagReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(tags ?? [new TagData("Nature", "outdoors")]);
        imageLocator.LocateAsync(page, Arg.Any<CancellationToken>()).Returns(imageUrl ?? new Option<string>.Some(WallpaperImageUrl));
        fileClassificationRepository.IsAlreadyDownloadedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(isAlreadyDownloaded);
        imageDownloader.DownloadAsync(page, WallpaperImageUrl, Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(downloadResult ?? Exceptional.Success(imageBytes));

        if (saveException is not null)
        {
            fileStore.SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns<SavedWallpaperFile>(_ => throw saveException);
        }
        else
        {
            fileStore.SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>()).Returns(savedFile ?? new SavedWallpaperFile("/root/base/pic.jpg", 3));
        }

        dimensionsReader.Read(Arg.Any<byte[]>()).Returns(dimensions ?? new ImageDimensions(0, 0));

        return new(tagReader, imageLocator, imageDownloader, dimensionsReader, fileStore, categoryRegistrar, fileClassificationRepository, clock, fileSystem);
    }
}
