using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Playwright;
using Testably.Abstractions.Testing;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Scraping;

public sealed class GivenSearchCategoryScrapeAction
{
    private readonly MockFileSystem fileSystem = new();
    private readonly IScrapeContextReader contextReader = Substitute.For<IScrapeContextReader>();
    private readonly ISearchCategoryWriter categoryWriter = Substitute.For<ISearchCategoryWriter>();
    private readonly IWallpaperCountReader countReader = Substitute.For<IWallpaperCountReader>();
    private readonly IWallpaperHrefCollector hrefCollector = Substitute.For<IWallpaperHrefCollector>();
    private readonly ITagReader tagReader = Substitute.For<ITagReader>();
    private readonly IWallpaperImageLocator imageLocator = Substitute.For<IWallpaperImageLocator>();
    private readonly IWallpaperImageDownloader imageDownloader = Substitute.For<IWallpaperImageDownloader>();
    private readonly IImageDimensionsReader dimensionsReader = Substitute.For<IImageDimensionsReader>();
    private readonly IWallpaperFileStore fileStore = Substitute.For<IWallpaperFileStore>();
    private readonly IWallpaperCategoryRegistrar categoryRegistrar = Substitute.For<IWallpaperCategoryRegistrar>();
    private readonly IWallpaperFileClassificationRepository fileClassificationRepository = Substitute.For<IWallpaperFileClassificationRepository>();
    private readonly IWallpaperThumbnailPublisher thumbnailPublisher = Substitute.For<IWallpaperThumbnailPublisher>();
    private readonly IProgress<string> progress = Substitute.For<IProgress<string>>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly Clock clock = () => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private const string WallpaperHref = "https://wallhaven.cc/w/abc123";
    private const string WallpaperImageUrl = "https://wallhaven.cc/images/pic.jpg";

    private static readonly byte[] imageBytes = [1, 2, 3];
    private static readonly string[] expectedNatureTagOnly = ["Nature"];

    private static readonly ScrapeContext singleCategoryContext = new(
        [new ScrapeCategory("Nature", "https://wallhaven.cc/search?categories=1", false, false)],
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

    private static readonly ScrapeContext twoCategoryContext = new(
        [
            new ScrapeCategory("Nature", "https://wallhaven.cc/search?categories=1", false, false),
            new ScrapeCategory("Space", "https://wallhaven.cc/search?categories=2", false, false),
        ],
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
        [new ScrapeCategory("Nature", "https://wallhaven.cc/search?categories=1", false, false)],
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
    public async Task when_a_category_has_more_wallpapers_than_fit_on_one_page_then_progress_reports_the_page_count_and_a_success_result_is_returned()
    {
        var sut = CreateSut(clock, wallpaperCount: 50);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Visiting category: <Run FontSize=\"18\">Nature</Run>")));
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("need to get all <Span Foreground=\"Green\">3</Span> pages")));
    }

    [Fact]
    public async Task when_persisting_scrape_progress_fails_then_progress_reports_the_failure_and_the_page_is_still_visited()
    {
        var sut = CreateSut(clock, writerResult: Result.Failure<FunctionalParadigm.Unit, string>("No search category named 'Nature' exists to update."));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to persist scrape progress for category: <Run FontSize=\"18\">Nature</Run>, error: <Span Foreground=\"Red\">No search category named 'Nature' exists to update.</Span>")));
        await page.Received().GotoAsync(Arg.Is<string>(url => url!.Contains("&page=1")));
    }

    [Fact]
    public async Task when_multiple_categories_are_configured_then_each_category_is_visited_on_its_own_search_url()
    {
        var sut = CreateSut(clock, context: twoCategoryContext);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Visiting category: <Run FontSize=\"18\">Nature</Run>")));
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Visiting category: <Run FontSize=\"18\">Space</Run>")));
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=2&page=1");
        await hrefCollector.Received(2).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_category_spans_multiple_pages_then_each_page_is_visited_and_hrefs_are_collected_per_page()
    {
        var sut = CreateSut(clock, wallpaperCount: 30);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=2");
        await hrefCollector.Received(2).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reading_the_scrape_context_fails_then_a_failure_result_is_returned_instead_of_throwing()
    {
        var sut = CreateSut(clock, contextReaderException: new InvalidOperationException("Sequence contains no elements"));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Failure<FunctionalParadigm.Unit>>();
    }

    [Fact]
    public async Task when_the_wallpaper_page_fails_to_load_then_progress_reports_the_failure_and_no_tags_are_read()
    {
        var sut = CreateSut(clock, hrefs: [WallpaperHref], wallpaperPageOk: false, wallpaperPageStatus: 404);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to load wallpaper page")));
        await tagReader.DidNotReceive().ReadAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_has_no_image_url_then_progress_reports_the_failure_and_nothing_is_downloaded()
    {
        var sut = CreateSut(clock, hrefs: [WallpaperHref], imageUrl: Option<string>.None.Instance);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to get wallpaper image URL")));
        await imageDownloader.DidNotReceive().DownloadAsync(Arg.Any<IPage>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_was_already_downloaded_then_its_page_is_never_visited_and_it_is_not_downloaded_again()
    {
        var sut = CreateSut(clock, hrefs: [WallpaperHref], isAlreadyDownloaded: true);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.DidNotReceive().GotoAsync(WallpaperHref, Arg.Any<PageGotoOptions>());
        await tagReader.DidNotReceive().ReadAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
        await imageDownloader.DidNotReceive().DownloadAsync(Arg.Any<IPage>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        await fileStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await fileClassificationRepository.DidNotReceive().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ImageDimensions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_checking_whether_a_wallpaper_is_already_downloaded_then_the_check_is_a_contains_match_on_the_wallpaper_id()
    {
        var sut = CreateSut(clock, hrefs: [WallpaperHref], imageUrl: Option<string>.None.Instance);

        await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        await fileClassificationRepository.Received().IsAlreadyDownloadedAsync("abc123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_tag_on_the_ignore_list_then_it_is_saved_under_the_curated_directory_path()
    {
        var sut = CreateSut(clock, context: ignoredTagContext, hrefs: [WallpaperHref], tags: [new TagData("Nature", "outdoors"), new TagData("Ignored", "outdoors")]);

        await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        await fileStore.Received().SaveAsync("/root/base/N/Nature", "pic.jpg", imageBytes, Arg.Any<CancellationToken>());
        await fileStore.DidNotReceive().SaveAsync("/root/base/N/Nature/Ignored", Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_new_wallpaper_is_visited_then_its_categories_are_registered_and_it_is_downloaded_saved_and_recorded()
    {
        var dimensions = new ImageDimensions(10, 20);
        var sut = CreateSut(clock, hrefs: [WallpaperHref], dimensions: dimensions);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await categoryRegistrar.Received().EnsureCategoriesExistAsync(Arg.Is<IReadOnlyList<TagData>>(tags => tags != null && tags.Any(tag => tag.Tag == "Nature")), Arg.Any<CancellationToken>());
        await fileClassificationRepository.Received().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), WallpaperImageUrl, Arg.Any<string>(), 3, dimensions, Arg.Any<CancellationToken>());
        await imageDownloader.Received().DownloadAsync(page, WallpaperImageUrl, "Nature", Arg.Is<IReadOnlyList<string>>(tags => tags!.SequenceEqual(expectedNatureTagOnly)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_download_fails_then_progress_reports_the_failure_and_nothing_is_saved_or_recorded()
    {
        var exception = new InvalidOperationException("Navigating to 'https://wallhaven.cc/images/pic.jpg' did not produce a response.");
        var sut = CreateSut(clock, hrefs: [WallpaperHref], downloadResult: Exceptional.Failure<byte[]>(exception));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Failed to download wallpaper image")));
        await fileStore.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
        await fileClassificationRepository.DidNotReceive().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ImageDimensions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_saving_the_downloaded_file_throws_then_progress_reports_the_real_exception_detail_and_execution_still_succeeds()
    {
        var sut = CreateSut(clock, hrefs: [WallpaperHref], saveException: new InvalidOperationException("disk full"));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("disk full")));
        await fileClassificationRepository.DidNotReceive().RecordAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ImageDimensions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_count_and_last_page_visited_both_match_the_stored_progress_then_the_category_is_skipped_and_progress_is_reported()
    {
        var sut = CreateSut(clock, wallpaperCount: 42, storedProgress: new SearchCategoryProgress(42, 2));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        progress.Received().Report(Arg.Is<string>(message => message!.Contains("Category: <Run FontSize=\"18\">Nature</Run>") && message!.Contains("already fully visited")));
        thumbnailPublisher.Received().PublishCategorySkipped("Nature");
        await page.DidNotReceive().GotoAsync(Arg.Is<string>(url => url!.Contains("&page=")), Arg.Any<PageGotoOptions>());
        await hrefCollector.DidNotReceive().CollectAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_count_matches_but_the_last_page_visited_is_behind_the_calculated_page_count_then_the_category_is_scraped_normally()
    {
        var sut = CreateSut(clock, wallpaperCount: 42, storedProgress: new SearchCategoryProgress(42, 1));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=2");
        await hrefCollector.Received(2).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_count_differs_from_the_stored_count_then_the_category_is_scraped_normally()
    {
        var sut = CreateSut(clock, wallpaperCount: 24, storedProgress: new SearchCategoryProgress(20, 1));

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await hrefCollector.Received(1).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_reading_the_stored_progress_returns_none_then_the_category_is_scraped_normally()
    {
        var sut = CreateSut(clock, wallpaperCount: 24);

        var result = await sut.ExecuteAsync(page, progress, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Success<FunctionalParadigm.Unit>>();
        await page.Received(1).GotoAsync("https://wallhaven.cc/search?categories=1&page=1");
        await hrefCollector.Received(1).CollectAsync(page, Arg.Any<CancellationToken>());
    }

    private SearchCategoryScrapeAction CreateSut(
        Clock clock,
        ScrapeContext? context = null,
        int wallpaperCount = 1,
        SearchCategoryProgress? storedProgress = null,
        IReadOnlyList<string>? hrefs = null,
        bool wallpaperPageOk = true,
        int wallpaperPageStatus = 200,
        IReadOnlyList<TagData>? tags = null,
        Option<string>? imageUrl = null,
        bool isAlreadyDownloaded = false,
        Result<FunctionalParadigm.Unit, string>? writerResult = null,
        Exceptional<byte[]>? downloadResult = null,
        SavedWallpaperFile? savedFile = null,
        ImageDimensions? dimensions = null,
        Exception? contextReaderException = null,
        Exception? saveException = null)
    {
        if (contextReaderException is not null)
        {
            contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns<ScrapeContext>(_ => throw contextReaderException);
        }
        else
        {
            contextReader.ReadAsync(Arg.Any<CancellationToken>()).Returns(context ?? singleCategoryContext);
        }

        countReader.ReadAsync(page, Arg.Any<CancellationToken>()).Returns(wallpaperCount);

        var searchCategoryReader = Substitute.For<ISearchCategoryReader>();
        searchCategoryReader.GetProgressAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(storedProgress is null ? Option.None<SearchCategoryProgress>() : new Option<SearchCategoryProgress>.Some(storedProgress));

        categoryWriter.WriteAsync(Arg.Any<SearchCategoryDto>(), Arg.Any<CancellationToken>())
            .Returns(writerResult ?? Result.Success<FunctionalParadigm.Unit, string>(FunctionalParadigm.Unit.Instance));

        if (hrefs is not null)
        {
            hrefCollector.CollectAsync(page, Arg.Any<CancellationToken>()).Returns(hrefs);
        }

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

        return new(contextReader, categoryWriter, countReader, searchCategoryReader, hrefCollector, tagReader, imageLocator, imageDownloader, dimensionsReader, fileStore, categoryRegistrar, fileClassificationRepository, thumbnailPublisher, clock, fileSystem);
    }
}
