using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Support;

public sealed class GivenATagsManager : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();

        seedContext.TagsToIgnore.AddRange(
            new TagToIgnoreEntity { Value = "banned-image", IgnoreImage = true },
            new TagToIgnoreEntity { Value = "banned-text", IgnoreImage = false });
        await seedContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public void when_constructed_then_tags_to_ignore_completely_contains_only_image_ignored_tags()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContext().Returns(_ => new AppDbContext(options));

        var sut = new TagsManager(contextFactory);

        sut.TagsToIgnoreCompletely.Tags.ShouldBe(["banned-image"]);
    }

    [Fact]
    public void when_constructed_then_tags_text_to_ignore_contains_only_non_image_ignored_tags()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContext().Returns(_ => new AppDbContext(options));

        var sut = new TagsManager(contextFactory);

        sut.TagsTextToIgnore.Tags.ShouldBe(["banned-text"]);
    }

    [Fact]
    public void when_constructed_then_only_one_db_context_is_created()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContext().Returns(_ => new AppDbContext(options));

        _ = new TagsManager(contextFactory);

        contextFactory.Received(1).CreateDbContext();
    }
}
