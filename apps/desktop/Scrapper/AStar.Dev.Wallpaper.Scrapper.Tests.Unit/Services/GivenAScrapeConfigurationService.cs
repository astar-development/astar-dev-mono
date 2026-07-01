using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenAScrapeConfigurationService : IAsyncLifetime
{
    private const string ExistingPassword      = "real-database-password";
    private const string ExistingSessionCookie = "real-session-cookie";
    private const string ExistingApiKey        = "real-api-key";
    private const string ExistingSqlite        = "Data Source=production.db";
    private const string ExistingCategoryId    = "existing-cat";

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
        seedContext.ScrapeConfiguration.Add(CreateInitialScrapeConfigurationEntity());
        await seedContext.SaveChangesAsync();

        factory = Substitute.For<IDbContextFactory<FilesContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new FilesContext(options)));

        sut = new ScrapeConfigurationService(factory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_exporting_then_context_is_created()
    {
        await sut.ExportScrapeConfigurationAsync(TestContext.Current.CancellationToken);

        await factory.Received(1).CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_then_context_is_created()
    {
        await sut.ImportScrapeConfigurationAsync(CreateImportEntity(), TestContext.Current.CancellationToken);

        await factory.Received(1).CreateDbContextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_importing_then_existing_entity_is_updated()
    {
        var importEntity = CreateImportEntity(sqlite: "Data Source=updated.db");

        await sut.ImportScrapeConfigurationAsync(importEntity, TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var result = await verifyCtx.ScrapeConfiguration
            .Include(x => x.ConnectionStrings)
            .FirstAsync(TestContext.Current.CancellationToken);
        result.ConnectionStrings.Sqlite.ShouldBe("Data Source=updated.db");
    }

    [Fact]
    public async Task when_importing_password_is_redacted_then_existing_password_is_preserved()
    {
        var importEntity = CreateImportEntity(password: ApplicationMetadata.Redacted);

        await sut.ImportScrapeConfigurationAsync(importEntity, TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var result = await verifyCtx.ScrapeConfiguration
            .Include(x => x.UserConfiguration)
            .FirstAsync(TestContext.Current.CancellationToken);
        result.UserConfiguration.Password.ShouldBe(ExistingPassword);
    }

    [Fact]
    public async Task when_importing_session_cookie_is_redacted_then_existing_session_cookie_is_preserved()
    {
        var importEntity = CreateImportEntity(sessionCookie: ApplicationMetadata.Redacted);

        await sut.ImportScrapeConfigurationAsync(importEntity, TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var result = await verifyCtx.ScrapeConfiguration
            .Include(x => x.UserConfiguration)
            .FirstAsync(TestContext.Current.CancellationToken);
        result.UserConfiguration.SessionCookie.ShouldBe(ExistingSessionCookie);
    }

    [Fact]
    public async Task when_importing_api_key_is_redacted_then_existing_api_key_is_preserved()
    {
        var importEntity = CreateImportEntity(apiKey: ApplicationMetadata.Redacted);

        await sut.ImportScrapeConfigurationAsync(importEntity, TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var result = await verifyCtx.ScrapeConfiguration
            .Include(x => x.SearchConfiguration)
            .FirstAsync(TestContext.Current.CancellationToken);
        result.SearchConfiguration.ApiKey.ShouldBe(ExistingApiKey);
    }

    [Fact]
    public async Task when_importing_then_search_categories_are_upserted_by_id()
    {
        var importEntity = CreateImportEntity(categories:
        [
            new SearchCategories { Id = ExistingCategoryId, Name = "Updated Name",       TotalPages = 10, IncludeInSearch = true },
            new SearchCategories { Id = "new-cat",          Name = "Brand New Category", TotalPages = 5,  IncludeInSearch = true }
        ]);

        await sut.ImportScrapeConfigurationAsync(importEntity, TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var result = await verifyCtx.ScrapeConfiguration
            .Include(x => x.SearchConfiguration)
                .ThenInclude(x => x.SearchCategories)
            .FirstAsync(TestContext.Current.CancellationToken);
        result.SearchConfiguration.SearchCategories.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_importing_new_search_category_then_it_is_added()
    {
        var importEntity = CreateImportEntity(categories:
        [
            new SearchCategories { Id = "new-cat", Name = "New Category", TotalPages = 5, IncludeInSearch = true }
        ]);

        await sut.ImportScrapeConfigurationAsync(importEntity, TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var result = await verifyCtx.ScrapeConfiguration
            .Include(x => x.SearchConfiguration)
                .ThenInclude(x => x.SearchCategories)
            .FirstAsync(TestContext.Current.CancellationToken);
        result.SearchConfiguration.SearchCategories.ShouldContain(c => c.Id == "new-cat");
    }

    [Fact]
    public async Task when_importing_existing_search_category_then_it_is_updated_not_duplicated()
    {
        var importEntity = CreateImportEntity(categories:
        [
            new SearchCategories { Id = ExistingCategoryId, Name = "Updated Category Name", TotalPages = 20, IncludeInSearch = true }
        ]);

        await sut.ImportScrapeConfigurationAsync(importEntity, TestContext.Current.CancellationToken);

        await using var verifyCtx = new FilesContext(options);
        var result = await verifyCtx.ScrapeConfiguration
            .Include(x => x.SearchConfiguration)
                .ThenInclude(x => x.SearchCategories)
            .FirstAsync(TestContext.Current.CancellationToken);
        result.SearchConfiguration.SearchCategories.Count.ShouldBe(1);
        result.SearchConfiguration.SearchCategories[0].Name.ShouldBe("Updated Category Name");
    }

    [Fact]
    public async Task when_exporting_returns_entity_with_connection_strings()
    {
        var result = await sut.ExportScrapeConfigurationAsync(TestContext.Current.CancellationToken);

        result.ConnectionStrings.ShouldNotBeNull();
        result.ConnectionStrings.Sqlite.ShouldBe(ExistingSqlite);
    }

    [Fact]
    public async Task when_exporting_returns_entity_with_user_configuration()
    {
        var result = await sut.ExportScrapeConfigurationAsync(TestContext.Current.CancellationToken);

        result.UserConfiguration.ShouldNotBeNull();
        result.UserConfiguration.Password.ShouldBe(ExistingPassword);
    }

    private static ScrapeConfigurationEntity CreateInitialScrapeConfigurationEntity() => new()
    {
        ConnectionStrings = new ConnectionStrings { Sqlite = ExistingSqlite },
        UserConfiguration = new UserConfiguration
        {
            LoginEmailAddress = "user@example.com",
            Username          = "testuser",
            Password          = ExistingPassword,
            SessionCookie     = ExistingSessionCookie
        },
        SearchConfiguration = new SearchConfiguration
        {
            BaseUrl          = "https://example.com",
            ApiKey           = ExistingApiKey,
            SearchCategories =
            [
                new SearchCategories
                {
                    Id              = ExistingCategoryId,
                    Name            = "Existing Category",
                    TotalPages      = 5,
                    IncludeInSearch = true
                }
            ]
        },
        ScrapeDirectories = new ScrapeDirectories { RootDirectory = "/tmp/scrape" }
    };

    private static ScrapeConfigurationEntity CreateImportEntity(
        string sqlite        = "Data Source=updated.db",
        string password      = "new-password",
        string sessionCookie = "new-session-cookie",
        string apiKey        = "new-api-key",
        List<SearchCategories>? categories = null) => new()
    {
        ConnectionStrings = new ConnectionStrings { Sqlite = sqlite },
        UserConfiguration = new UserConfiguration
        {
            LoginEmailAddress = "updated@example.com",
            Username          = "updateduser",
            Password          = password,
            SessionCookie     = sessionCookie
        },
        SearchConfiguration = new SearchConfiguration
        {
            BaseUrl          = "https://updated.com",
            ApiKey           = apiKey,
            SearchCategories = categories ?? []
        },
        ScrapeDirectories = new ScrapeDirectories { RootDirectory = "/tmp/updated" }
    };
}
