using AStarDev.ControlDb.Files;
using AStarDev.ControlDb.ScrapeConfiguration;
using ImmutableDomain.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsUnit;

public sealed class GivenAControlDbContext : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"files-db-context-{Guid.NewGuid():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAControlDbContext()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
    }

    [Fact]
    public void when_the_model_is_built_then_no_exception_is_thrown() => context.Model.ShouldNotBeNull();

    [Fact]
    public void when_accessed_the_files_repository_should_be_an_immutable_repository() => context.FilesRepository.ShouldBeAssignableTo<IImmutableEntityRepository<FileEntity>>();


    [Fact]
    public void when_accessed_the_scrape_configuration_repository_should_be_an_immutable_repository() => context.ScrapeConfigurationRepository.ShouldBeAssignableTo<IImmutableEntityRepository<ScrapeConfigurationEntity>>();

    [Fact]
    public async Task when_the_database_is_created_then_a_file_entity_with_related_details_can_be_saved_and_reloaded()
    {
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var fileId = new FileId(Guid.Empty);
        var fileEntity = CreateFileEntity(fileId);
        await context.FilesRepository.AddImmutableAsync(fileEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reloaded = await context.FilesRepository.FindImmutableAsync(fileEntity.Path, fileEntity.Name);

        reloaded.ShouldNotBeNull();
        reloaded.Name.ShouldBe(fileEntity.Name);
        reloaded.Path.ShouldBe(fileEntity.Path);
        reloaded.Handle.ShouldBe(fileEntity.Handle);
    }

    [Fact]
    public async Task when_the_database_is_created_then_a_scrape_configuration_entity_with_related_details_can_be_saved_and_reloaded()
    {
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var scrapeConfigurationEntity = CreateScrapeConfigurationEntity();
        await context.ScrapeConfigurationRepository.AddImmutableAsync(scrapeConfigurationEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reloaded = await context.ScrapeConfigurationRepository.FindImmutableAsync(scrapeConfigurationEntity.Id);

        reloaded.ShouldNotBeNull();
        reloaded.ConnectionStrings.Sqlite.ShouldBe(scrapeConfigurationEntity.ConnectionStrings.Sqlite);
        reloaded.UserConfiguration.EmailAddress.ShouldBe(scrapeConfigurationEntity.UserConfiguration.EmailAddress);
        reloaded.SearchConfiguration.Category.ShouldBe(scrapeConfigurationEntity.SearchConfiguration.Category);
    }

    private static FileEntity CreateFileEntity(FileId fileId)
    {
        return new FileEntity(fileId, new FileName("wallpaper.jpg"), new DirectoryPath("/pictures/wallpapers"), new FileHandle("handle-1"), 1024)
        {
            FileAccessDetail = new FileAccessDetailEntity(new FileAccessDetailId(Guid.Empty), fileId, null, null, false),
            DeletionStatus = new DeletionStatusEntity(new DeletionStatusId(Guid.Empty), fileId, null, null, null),
        };
    }

    private static ScrapeConfigurationEntity CreateScrapeConfigurationEntity()
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.Empty);
        var connectionStringId = new ConnectionStringId(Guid.Empty);
        var userConfigurationId = new UserConfigurationId(Guid.Empty);
        var searchConfigurationId = new SearchConfigurationId(Guid.Empty);
        var scrapeDirectoriesId = new ScrapeDirectoriesId(Guid.Empty);
        var scrapeConfiguration = new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            ConnectionStrings = new ConnectionStringsEntity(connectionStringId, scrapeConfigurationId, "connection-string"),
            UserConfiguration = new UserConfigurationEntity(userConfigurationId, scrapeConfigurationId, "user@example.com", "username", "password", "session-cookie"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "search-config", "mock-category", 10),
            ScrapeDirectories = new ScrapeDirectoriesEntity(scrapeDirectoriesId, scrapeConfigurationId, "scrape-directory", "base-save-directory", "base-directory", "base-directory-famous", "sub-directory-name")
        };

        return scrapeConfiguration;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        context.Dispose();
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
