using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Repositories;

public sealed class GivenADatabaseResetRepository : IAsyncLifetime
{
    private const string FirstBaseSaveDirectory = "/old/save/dir";
    private const string LastBaseSaveDirectory = "/new/save/dir";

    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;
    private IDbContextFactory<AppDbContext> factory = null!;
    private DatabaseResetRepository sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();
        seedContext.ScrapeConfiguration.AddRange(
            CreateScrapeConfigurationEntity(baseSaveDirectory: FirstBaseSaveDirectory),
            CreateScrapeConfigurationEntity(baseSaveDirectory: LastBaseSaveDirectory));
        await seedContext.SaveChangesAsync();

        factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new AppDbContext(options)));

        sut = new DatabaseResetRepository(factory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_multiple_directories_exist_then_returns_base_save_directory_from_record_with_highest_id()
    {
        string? result = await sut.GetBaseSaveDirectoryAsync(CancellationToken.None);

        result.ShouldBe(LastBaseSaveDirectory);
    }

    [Fact]
    public async Task when_deleting_all_files_then_file_and_linked_detail_tables_are_cleared()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using (var arrangeContext = new AppDbContext(options))
        {
            var fileAccessDetail = new FileAccessDetailEntity();
            var deletionStatus = new DeletionStatusEntity();

            _ = await arrangeContext.FileAccessDetails.AddAsync(fileAccessDetail, cancellationToken);
            _ = await arrangeContext.Set<DeletionStatusEntity>().AddAsync(deletionStatus, cancellationToken);

            _ = await arrangeContext.Files.AddAsync(new FileDetailEntity
            {
                DirectoryName = new DirectoryName("/tmp"),
                FileName = new FileName("image.jpg"),
                FileHandle = FileHandleFactory.Create("image.jpg"),
                FileSize = 100,
                IsImage = true,
                ImageDetail = new ImageDetailEntity { Width = 1920, Height = 1080 },
                FileAccessDetail = fileAccessDetail,
                DeletionStatus = deletionStatus
            }, cancellationToken);

            _ = await arrangeContext.SaveChangesAsync(cancellationToken);
        }

        await sut.DeleteAllFilesAsync(cancellationToken);

        await using var assertContext = new AppDbContext(options);
        (await assertContext.Files.CountAsync(cancellationToken)).ShouldBe(0);
        (await assertContext.Set<ImageDetailEntity>().CountAsync(cancellationToken)).ShouldBe(0);
        (await assertContext.FileAccessDetails.CountAsync(cancellationToken)).ShouldBe(0);
        (await assertContext.Set<DeletionStatusEntity>().CountAsync(cancellationToken)).ShouldBe(0);
    }

    private static ScrapeConfigurationEntity CreateScrapeConfigurationEntity(string baseSaveDirectory) => new()
    {
        ConnectionStrings = new ConnectionStringsEntity { Sqlite = "Data Source=test.db" },
        UserConfiguration = new UserConfigurationEntity { LoginEmailAddress = "user@example.com", Username = "testuser", Password = "password", SessionCookie = "cookie" },
        SearchConfiguration = new SearchConfigurationEntity { BaseUrl = new Uri("https://example.com"), ApiKey = "key" },
        ScrapeDirectories = new ScrapeDirectoriesEntity { BaseSaveDirectory = baseSaveDirectory }
    };
}
