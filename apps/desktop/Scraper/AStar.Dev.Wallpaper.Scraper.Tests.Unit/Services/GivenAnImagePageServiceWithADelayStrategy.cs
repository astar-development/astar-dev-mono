using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Pages;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.Support;
using AStar.Dev.Wallpaper.Scraper.Tests.Unit.TestData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenAnImagePageServiceWithADelayStrategy : IAsyncLifetime
{
    private const string Link = "https://example.test/w/12345";
    private const string CategoryName = "Cars";
    private const string CategoryId = "cat-1";
    private const string ImageUrl = "https://example.test/images/12345.data";

    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    private static ImagePage BuildImagePageReturningScrapedImage(string imageUrl, string tagText = "Nature", string? tagCategory = "Landscape")
    {
        var tagLocator = Substitute.For<ILocator>();
        tagLocator.InnerTextAsync().Returns(Task.FromResult(tagText));
        tagLocator.GetAttributeAsync("original-title").Returns(Task.FromResult(tagCategory));

        var tagsLocator = Substitute.For<ILocator>();
        tagsLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([tagLocator,]));

        var imageLocator = Substitute.For<ILocator>();
        imageLocator.GetAttributeAsync("src").Returns(Task.FromResult<string?>(imageUrl));

        var page = Substitute.For<IPage>();
        page.Locator(".tagname", Arg.Any<PageLocatorOptions>()).Returns(tagsLocator);
        page.Locator("#wallpaper", Arg.Any<PageLocatorOptions>()).Returns(imageLocator);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();

        return new ImagePage(playwrightService, scrapeConfiguration, new(), new());
    }

    private static ImagePage BuildImagePageReturningSkippedImage()
    {
        var tagLocator = Substitute.For<ILocator>();
        tagLocator.InnerTextAsync().Returns(Task.FromResult("BadCategory"));
        tagLocator.GetAttributeAsync("original-title").Returns(Task.FromResult<string?>("ExcludedCategory"));

        var tagsLocator = Substitute.For<ILocator>();
        tagsLocator.AllAsync().Returns(Task.FromResult<IReadOnlyList<ILocator>>([tagLocator,]));

        var page = Substitute.For<IPage>();
        page.Locator(".tagname", Arg.Any<PageLocatorOptions>()).Returns(tagsLocator);

        var playwrightService = Substitute.For<IPlaywrightService>();
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(page));

        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();

        return new ImagePage(playwrightService, scrapeConfiguration, new() { Tags = ["ExcludedCategory",], }, new());
    }

    private ImagePageService BuildService(
        ImagePage imagePage,
        IFileDetailRepository fileDetailRepository,
        IDelayStrategy delayStrategy,
        IImageRetriever imageRetriever,
        IImageSaver imageSaver,
        MockFileSystem fileSystem,
        IDirectoryHelper directoryHelper,
        IFileClassificationCategoriesRepository? scrapedTagRepository = null,
        IImageDimensionReader? imageDimensionReader = null)
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var fileClassificationService = new FileClassificationService(contextFactory);
        var scrapeConfiguration = new ScrapeConfigurationBuilder().Build();

        return new ImagePageService(
            imagePage,
            fileDetailRepository,
            fileClassificationService,
            scrapeConfiguration,
            System.TimeProvider.System,
            new LoggerConfiguration().CreateLogger(),
            directoryHelper,
            new(),
            delayStrategy,
            imageRetriever,
            imageSaver,
            fileSystem,
            scrapedTagRepository ?? RepositoryTestDoubles.BuildScrapedTagRepository(),
            imageDimensionReader ?? BuildDefaultImageDimensionReader());
    }

    private static IImageDimensionReader BuildDefaultImageDimensionReader()
    {
        var imageDimensionReader = Substitute.For<IImageDimensionReader>();
        imageDimensionReader.Read(Arg.Any<byte[]>(), Arg.Any<string>())
                             .Returns(Result.Failure<ImageDimensions, ScrapeError>(ScrapeErrorFactory.CreateImageDimensionReadFailed("unset", "dimension reader not under test")));

        return imageDimensionReader;
    }

    private static IImageSaver BuildSucceedingImageSaver()
    {
        var imageSaver = Substitute.For<IImageSaver>();
        imageSaver.SaveAsync(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Task.FromResult(Result.Success<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(global::AStar.Dev.FunctionalParadigm.Unit.Value)));

        return imageSaver;
    }

    [Fact]
    public async Task when_the_file_already_exists_then_the_delay_strategy_receives_the_image_already_downloaded_delay()
    {
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository(exists: true);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);

        var sut = BuildService(imagePage, fileDetailRepository, delayStrategy, Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IDirectoryHelper>());

        await sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken);

        await delayStrategy.Received(1).DelayAsync(DelayKind.ImageAlreadyDownloaded, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_file_does_not_already_exist_then_the_delay_strategy_receives_the_before_image_delay()
    {
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository(exists: false);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/save/dir");
        fileSystem.File.WriteAllBytes("/save/dir/12345.data", [1, 2, 3,]);

        var sut = BuildService(imagePage, fileDetailRepository, delayStrategy, imageRetriever, BuildSucceedingImageSaver(), fileSystem, directoryHelper);

        await sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken);

        await delayStrategy.Received(1).DelayAsync(DelayKind.BeforeImage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_retriever_always_fails_then_get_the_image_pages_async_retries_exactly_once_before_returning_a_failure()
    {
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository(exists: false);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(Result.Failure<byte[], ScrapeError>(ScrapeErrorFactory.CreateImageDownloadFailed(ImageUrl, "download failed"))));
        var fileSystem = new MockFileSystem();

        var sut = BuildService(imagePage, fileDetailRepository, delayStrategy, imageRetriever, Substitute.For<IImageSaver>(), fileSystem, directoryHelper);

        var exception = await Record.ExceptionAsync(() => sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken));

        exception.ShouldBeNull();
        await imageRetriever.Received(2).GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_retriever_always_fails_then_the_delay_strategy_receives_exactly_one_retry_delay()
    {
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository(exists: false);
        var delayStrategy = Substitute.For<IDelayStrategy>();
        delayStrategy.DelayAsync(Arg.Any<DelayKind>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(Result.Failure<byte[], ScrapeError>(ScrapeErrorFactory.CreateImageDownloadFailed(ImageUrl, "download failed"))));
        var fileSystem = new MockFileSystem();

        var sut = BuildService(imagePage, fileDetailRepository, delayStrategy, imageRetriever, Substitute.For<IImageSaver>(), fileSystem, directoryHelper);

        await sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken);

        await delayStrategy.Received(1).DelayAsync(DelayKind.Retry, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_retriever_always_fails_then_the_final_result_is_an_image_download_failed_error()
    {
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository(exists: false);
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult(Result.Failure<byte[], ScrapeError>(ScrapeErrorFactory.CreateImageDownloadFailed(ImageUrl, "download failed"))));

        var sut = BuildService(imagePage, fileDetailRepository, new NoOpDelayStrategy(), imageRetriever, Substitute.For<IImageSaver>(), new MockFileSystem(), directoryHelper);

        var result = await sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken);

        var error = result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<ImageDownloadFailed>();
        error.ImageUrl.ShouldBe(ImageUrl);
    }

    [Fact]
    public async Task when_the_image_retriever_fails_once_then_succeeds_then_the_final_result_is_ok()
    {
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository(exists: false);
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        int attempts = 0;
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                      .Returns(_ =>
                      {
                          attempts++;

                          return Task.FromResult(attempts == 1
                              ? Result.Failure<byte[], ScrapeError>(ScrapeErrorFactory.CreateImageDownloadFailed(ImageUrl, "download failed"))
                              : Result.Success<byte[], ScrapeError>([1, 2, 3,]));
                      });
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/save/dir");

        var sut = BuildService(imagePage, fileDetailRepository, new NoOpDelayStrategy(), imageRetriever, BuildSucceedingImageSaver(), fileSystem, directoryHelper);

        var result = await sut.GetTheImagePagesAsync([Link,], CategoryId, CategoryName, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_processing_a_scraped_image_then_the_scraped_tag_repository_is_saved_to_exactly_once()
    {
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/save/dir");
        var scrapedTagRepository = RepositoryTestDoubles.BuildScrapedTagRepository();

        var sut = BuildService(imagePage, RepositoryTestDoubles.BuildFileDetailRepository(), new NoOpDelayStrategy(), imageRetriever, BuildSucceedingImageSaver(), fileSystem, directoryHelper, scrapedTagRepository);

        await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        await scrapedTagRepository.Received(1).SaveAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_processing_a_skipped_image_then_the_scraped_tag_repository_is_saved_to_exactly_once()
    {
        var imagePage = BuildImagePageReturningSkippedImage();
        var scrapedTagRepository = RepositoryTestDoubles.BuildScrapedTagRepository();

        var sut = BuildService(imagePage, RepositoryTestDoubles.BuildFileDetailRepository(), new NoOpDelayStrategy(), Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IDirectoryHelper>(), scrapedTagRepository);

        await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        await scrapedTagRepository.Received(1).SaveAsync(Arg.Any<IReadOnlyList<TagData>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_processing_a_skipped_image_then_the_image_retriever_is_never_called()
    {
        var imagePage = BuildImagePageReturningSkippedImage();
        var imageRetriever = Substitute.For<IImageRetriever>();

        var sut = BuildService(imagePage, RepositoryTestDoubles.BuildFileDetailRepository(), new NoOpDelayStrategy(), imageRetriever, Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IDirectoryHelper>());

        await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        await imageRetriever.DidNotReceive().GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_processing_a_skipped_image_then_the_result_is_ok()
    {
        var imagePage = BuildImagePageReturningSkippedImage();

        var sut = BuildService(imagePage, RepositoryTestDoubles.BuildFileDetailRepository(), new NoOpDelayStrategy(), Substitute.For<IImageRetriever>(), Substitute.For<IImageSaver>(), new MockFileSystem(), Substitute.For<IDirectoryHelper>());

        var result = await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
    }

    [Fact]
    public async Task when_processing_a_scraped_image_then_the_file_detail_repository_receives_exactly_one_add_call()
    {
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/save/dir");
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository();

        var sut = BuildService(imagePage, fileDetailRepository, new NoOpDelayStrategy(), imageRetriever, BuildSucceedingImageSaver(), fileSystem, directoryHelper);

        await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        await fileDetailRepository.Received(1).AddAsync(Arg.Any<FileDetailEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_dimension_reader_returns_dimensions_then_the_persisted_file_detail_carries_them()
    {
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/save/dir");
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository();
        var imageDimensionReader = Substitute.For<IImageDimensionReader>();
        imageDimensionReader.Read(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Result.Success<ImageDimensions, ScrapeError>(new ImageDimensions(40, 20)));

        var sut = BuildService(imagePage, fileDetailRepository, new NoOpDelayStrategy(), imageRetriever, BuildSucceedingImageSaver(), fileSystem, directoryHelper, imageDimensionReader: imageDimensionReader);

        await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        await fileDetailRepository.Received(1).AddAsync(Arg.Is<FileDetailEntity>(detail => detail.ImageDetail.Width == 40 && detail.ImageDetail.Height == 20), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_image_dimension_reader_fails_then_the_file_detail_is_still_persisted()
    {
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/save/dir");
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository();
        var imageDimensionReader = Substitute.For<IImageDimensionReader>();
        imageDimensionReader.Read(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Result.Failure<ImageDimensions, ScrapeError>(ScrapeErrorFactory.CreateImageDimensionReadFailed("/save/dir/12345.data", "invalid image")));

        var sut = BuildService(imagePage, fileDetailRepository, new NoOpDelayStrategy(), imageRetriever, BuildSucceedingImageSaver(), fileSystem, directoryHelper, imageDimensionReader: imageDimensionReader);

        var result = await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
        await fileDetailRepository.Received(1).AddAsync(Arg.Any<FileDetailEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_scraped_file_is_not_an_image_extension_then_the_dimension_reader_is_still_invoked()
    {
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/save/dir");
        var imageDimensionReader = Substitute.For<IImageDimensionReader>();
        imageDimensionReader.Read(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Result.Failure<ImageDimensions, ScrapeError>(ScrapeErrorFactory.CreateImageDimensionReadFailed("/save/dir/12345.data", "dimension reader not under test")));

        var sut = BuildService(imagePage, RepositoryTestDoubles.BuildFileDetailRepository(), new NoOpDelayStrategy(), imageRetriever, BuildSucceedingImageSaver(), fileSystem, directoryHelper, imageDimensionReader: imageDimensionReader);

        await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        imageDimensionReader.Received(1).Read(Arg.Any<byte[]>(), Arg.Any<string>());
    }

    [Fact]
    public async Task when_the_image_saver_fails_then_an_image_save_failed_error_is_returned()
    {
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var imageSaver = Substitute.For<IImageSaver>();
        imageSaver.SaveAsync(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Task.FromResult(Result.Failure<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(ScrapeErrorFactory.CreateImageSaveFailed("/save/dir/12345.data", "disk full"))));

        var sut = BuildService(imagePage, RepositoryTestDoubles.BuildFileDetailRepository(), new NoOpDelayStrategy(), imageRetriever, imageSaver, new MockFileSystem(), directoryHelper);

        var result = await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Fail<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>().Error.ShouldBeOfType<ImageSaveFailed>();
    }

    [Fact]
    public async Task when_the_image_saver_fails_then_the_file_detail_repository_is_never_called()
    {
        var imagePage = BuildImagePageReturningScrapedImage(ImageUrl);
        var directoryHelper = Substitute.For<IDirectoryHelper>();
        directoryHelper.CreateDirectoryIfRequired(Arg.Any<List<string>>()).Returns(new DirectoryName("/save/dir"));
        var imageRetriever = Substitute.For<IImageRetriever>();
        imageRetriever.GetImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success<byte[], ScrapeError>([1, 2, 3,])));
        var imageSaver = Substitute.For<IImageSaver>();
        imageSaver.SaveAsync(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(Task.FromResult(Result.Failure<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>(ScrapeErrorFactory.CreateImageSaveFailed("/save/dir/12345.data", "disk full"))));
        var fileDetailRepository = RepositoryTestDoubles.BuildFileDetailRepository();

        var sut = BuildService(imagePage, fileDetailRepository, new NoOpDelayStrategy(), imageRetriever, imageSaver, new MockFileSystem(), directoryHelper);

        await sut.ProcessImagePageAsync(Link, CategoryName, new PageClassificationData([], null, []), TestContext.Current.CancellationToken);

        await fileDetailRepository.DidNotReceive().AddAsync(Arg.Any<FileDetailEntity>(), Arg.Any<CancellationToken>());
    }
}
