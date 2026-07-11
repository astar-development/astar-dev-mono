using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Repositories;

public sealed class GivenAFileDetailRepository : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;
    private FileDetailRepository sut = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var seedContext = new AppDbContext(options);
        await seedContext.Database.MigrateAsync();
        seedContext.Files.Add(new FileDetailEntity { FileName = new FileName("image.png"), DirectoryName = new DirectoryName("/tmp"), FileHandle = FileHandleFactory.Create("image.png") });
        await seedContext.SaveChangesAsync();

        var factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));

        sut = new FileDetailRepository(factory);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_a_file_with_the_exact_name_exists_then_exists_returns_true()
    {
        var result = await sut.ExistsAsync("image.png", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<bool, ScrapeError>>().Value.ShouldBeTrue();
    }

    [Fact]
    public async Task when_no_file_has_the_exact_name_but_a_substring_matches_then_exists_returns_false()
    {
        var result = await sut.ExistsAsync("mage.pn", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<bool, ScrapeError>>().Value.ShouldBeFalse();
    }

    [Fact]
    public async Task when_no_file_matches_then_exists_returns_false()
    {
        var result = await sut.ExistsAsync("missing.png", TestContext.Current.CancellationToken);

        result.ShouldBeOfType<Ok<bool, ScrapeError>>().Value.ShouldBeFalse();
    }
}
