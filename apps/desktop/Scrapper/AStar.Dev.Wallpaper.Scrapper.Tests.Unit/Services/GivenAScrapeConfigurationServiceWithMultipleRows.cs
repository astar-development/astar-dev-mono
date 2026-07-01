using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAScrapeConfigurationServiceWithMultipleRows : IAsyncLifetime
{
    private const string OlderSqlite    = "Data Source=old.db";
    private const string NewerSqlite    = "Data Source=new.db";
    private const string ImportedSqlite = "Data Source=imported.db";

    private SqliteConnection connection = null!;
    private DbContextOptions<FilesContext> options = null!;
    private IDbContextFactory<FilesContext> factory = null!;
    private ScrapeConfigurationService sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<FilesContext>()
            .UseSqlite(connection)
            .Options;

        await using var seedContext = new FilesContext(options);
        await seedContext.Database.MigrateAsync();

        seedContext.ScrapeConfiguration.Add(CreateScrapeConfigEntity(OlderSqlite));
        await seedContext.SaveChangesAsync();

        seedContext.ScrapeConfiguration.Add(CreateScrapeConfigEntity(NewerSqlite));
        await seedContext.SaveChangesAsync();

        factory = Substitute.For<IDbContextFactory<FilesContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new FilesContext(options)));

        sut = new ScrapeConfigurationService(factory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_exporting_with_multiple_configuration_rows_then_the_newest_entity_is_returned()
    {
        var result = await sut.ExportScrapeConfigurationAsync(TestContext.Current.CancellationToken);

        result.ConnectionStrings.Sqlite.ShouldBe(NewerSqlite);
    }

    [Fact]
    public async Task when_importing_with_multiple_configuration_rows_then_the_newest_entity_is_updated()
    {
        var incoming = CreateScrapeConfigEntity(ImportedSqlite);

        await sut.ImportScrapeConfigurationAsync(incoming, TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var newestEntity = await verifyCtx.ScrapeConfiguration
            .Include(x => x.ConnectionStrings)
            .OrderByDescending(e => e.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        newestEntity.ConnectionStrings.Sqlite.ShouldBe(ImportedSqlite);

        var oldestEntity = await verifyCtx.ScrapeConfiguration
            .Include(x => x.ConnectionStrings)
            .OrderBy(e => e.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        oldestEntity.ConnectionStrings.Sqlite.ShouldBe(OlderSqlite);
    }

    private static ScrapeConfigurationEntity CreateScrapeConfigEntity(string sqlite) => new()
    {
        ConnectionStrings   = new ConnectionStrings   { Sqlite = sqlite },
        UserConfiguration   = new UserConfiguration   { LoginEmailAddress = "user@example.com", Username = "user", Password = "password", SessionCookie = "cookie" },
        SearchConfiguration = new SearchConfiguration { BaseUrl = "https://example.com" },
        ScrapeDirectories   = new ScrapeDirectories   { RootDirectory = "/tmp" }
    };
}
