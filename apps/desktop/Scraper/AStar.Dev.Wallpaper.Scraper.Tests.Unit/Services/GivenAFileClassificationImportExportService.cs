using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenAFileClassificationImportExportService : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;
    private IDbContextFactory<AppDbContext> factory = null!;
    private FileClassificationImportExportService sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();

        factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromResult(new AppDbContext(options)));

        sut = new FileClassificationImportExportService(factory, new LoggerConfiguration().CreateLogger());
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_exporting_classifications_then_categories_and_keywords_are_returned()
    {
        await using var seedCtx = new AppDbContext(options);
        var classification = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, IncludeInSearch = true };
        seedCtx.FileClassificationCategories.Add(classification);
        seedCtx.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "animals", Category = classification });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.ExportClassificationsAsync(TestContext.Current.CancellationToken);

        result.Categories.ShouldContain(c => c.Name == "Animals");
        result.Keywords.ShouldHaveSingleItem().Keyword.ShouldBe("animals");
    }

    [Fact]
    public async Task when_importing_a_new_classification_then_it_is_added()
    {
        var incoming = (
            Categories: new List<FileClassificationCategoryEntity> { new() { Id = 2, Name = "Animals", Level = 1, IncludeInSearch = true } },
            Keywords: new List<FileClassificationKeywordEntity> { new() { CategoryId = 2, Keyword = "animals" } });

        var result = await sut.ImportClassificationsAsync(incoming, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
        await using var verifyCtx = new AppDbContext(options);
        var stored = await verifyCtx.FileClassificationCategories.SingleAsync(c => c.Name == "Animals", TestContext.Current.CancellationToken);
        stored.Name.ShouldBe("Animals");
    }

    [Fact]
    public async Task when_importing_a_classification_whose_keyword_already_exists_with_different_casing_then_no_duplicate_keyword_is_added()
    {
        await using var seedCtx = new AppDbContext(options);
        var existing = new FileClassificationCategoryEntity { Name = "Animals", Level = 3, IncludeInSearch = true };
        seedCtx.FileClassificationCategories.Add(existing);
        seedCtx.FileClassificationKeywords.Add(new FileClassificationKeywordEntity { Keyword = "ANIMALS", Category = existing });
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var incoming = (
            Categories: new List<FileClassificationCategoryEntity> { new() { Id = existing.Id, Name = "Animals", Level = 3, IncludeInSearch = true } },
            Keywords: new List<FileClassificationKeywordEntity> { new() { CategoryId = existing.Id, Keyword = "animals" } });

        var result = await sut.ImportClassificationsAsync(incoming, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
        await using var verifyCtx = new AppDbContext(options);
        int count = await verifyCtx.FileClassificationKeywords.CountAsync(k => k.CategoryId == existing.Id, TestContext.Current.CancellationToken);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task when_importing_a_classification_whose_primary_upsert_fails_then_it_is_reparented_under_unclassified()
    {
        await using var seedCtx = new AppDbContext(options);
        var unclassifiedRoot = new FileClassificationCategoryEntity { Name = "Unclassified", Level = 1 };
        seedCtx.FileClassificationCategories.Add(unclassifiedRoot);
        await seedCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var incoming = (
            Categories: new List<FileClassificationCategoryEntity> { new() { Id = 999, Name = "Animals", Level = 2, ParentId = 999999, IncludeInSearch = true } },
            Keywords: new List<FileClassificationKeywordEntity>());

        var result = await sut.ImportClassificationsAsync(incoming, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<global::AStar.Dev.FunctionalParadigm.Unit, ScrapeError>>();
        await using var verifyCtx = new AppDbContext(options);
        var stored = await verifyCtx.FileClassificationCategories.SingleAsync(c => c.Name == "Animals", TestContext.Current.CancellationToken);
        stored.ParentId.ShouldBe(unclassifiedRoot.Id);
    }
}
