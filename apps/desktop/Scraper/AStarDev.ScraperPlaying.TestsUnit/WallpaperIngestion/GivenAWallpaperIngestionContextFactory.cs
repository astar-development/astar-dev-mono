using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperIngestionContextFactory
{
    private static readonly string[] personCategories = ["Celebrities"];
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly WallpaperIngestionContextFactory factory;

    public GivenAWallpaperIngestionContextFactory()
    {
        _ = unitOfWork.Register<FileEntity, FileId>();
        factory = new(new StubClientFactory(), unitOfWork, new StubSaveDirectoryResolver());
    }

    [Fact]
    public async Task when_no_category_is_supplied_then_the_label_falls_back_to_top_wallpapers()
    {
        var context = await factory.CreateAsync(CreateRequest(Option.None<string>()), CancellationToken.None);

        context.Output.CategoryLabel.ShouldBe("Top Wallpapers");
    }

    [Fact]
    public async Task when_a_category_is_supplied_then_the_label_is_the_category_name()
    {
        var context = await factory.CreateAsync(CreateRequest(Option.Some("Cars")), CancellationToken.None);

        context.Output.CategoryLabel.ShouldBe("Cars");
    }

    [Fact]
    public async Task when_a_context_is_created_then_it_carries_the_resolved_directories_and_the_person_categories()
    {
        var context = await factory.CreateAsync(CreateRequest(Option.Some("Cars")), CancellationToken.None);

        (context.Output.Directories, context.PersonCategories).ShouldBe((new SaveDirectories("root", "famous", "segment"), (IReadOnlyList<string>)personCategories));
    }

    private static PageScrapeRequest CreateRequest(Option<string> categoryName)
        => new(new ScrapeLabel("label", categoryName), Option.None<SearchCategoryProgress>(), new PageHooks(_ => { }, page => new Uri($"https://example.test/page/{page}")), new ScrapeTarget(new WallhavenConnection("api-key", new Uri("https://example.test")), personCategories));

    private sealed class StubClientFactory : IWallhavenClientFactory
    {
        public HttpClient Create(WallhavenConnection connection) => new();
    }

    private sealed class StubSaveDirectoryResolver : ISaveDirectoryResolver
    {
        public Task<SaveDirectories> ResolveSaveDirectoriesAsync(Option<string> categoryName, CancellationToken cancellationToken) => Task.FromResult(new SaveDirectories("root", "famous", "segment"));
    }
}
