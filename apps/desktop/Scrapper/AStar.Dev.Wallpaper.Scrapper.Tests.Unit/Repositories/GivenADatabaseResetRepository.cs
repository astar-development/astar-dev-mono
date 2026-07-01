using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Repositories;

public sealed class GivenADatabaseResetRepository : IAsyncLifetime
{
    private const string FirstBaseSaveDirectory = "/old/save/dir";
    private const string LastBaseSaveDirectory = "/new/save/dir";

    private SqliteConnection connection = null!;
    private DbContextOptions<FilesContext> options = null!;
    private IDbContextFactory<FilesContext> factory = null!;
    private DatabaseResetRepository sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<FilesContext>()
            .UseSqlite(connection)
            .Options;

        await using var seedContext = new FilesContext(options);
        await seedContext.Database.MigrateAsync();
        seedContext.ScrapeConfiguration.AddRange(
            CreateScrapeConfigurationEntity(baseSaveDirectory: FirstBaseSaveDirectory),
            CreateScrapeConfigurationEntity(baseSaveDirectory: LastBaseSaveDirectory));
        await seedContext.SaveChangesAsync();

        factory = Substitute.For<IDbContextFactory<FilesContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new FilesContext(options)));

        sut = new DatabaseResetRepository(factory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_multiple_directories_exist_then_returns_base_save_directory_from_record_with_highest_id()
    {
        var result = await sut.GetBaseSaveDirectoryAsync(CancellationToken.None);

        result.ShouldBe(LastBaseSaveDirectory);
    }

    private static ScrapeConfigurationEntity CreateScrapeConfigurationEntity(string baseSaveDirectory) => new()
    {
        ConnectionStrings = new ConnectionStrings { Sqlite = "Data Source=test.db" },
        UserConfiguration = new UserConfiguration { LoginEmailAddress = "user@example.com", Username = "testuser", Password = "password", SessionCookie = "cookie" },
        SearchConfiguration = new SearchConfiguration { BaseUrl = new Uri("https://example.com"), ApiKey = "key" },
        ScrapeDirectories = new ScrapeDirectories { BaseSaveDirectory = baseSaveDirectory }
    };
}
