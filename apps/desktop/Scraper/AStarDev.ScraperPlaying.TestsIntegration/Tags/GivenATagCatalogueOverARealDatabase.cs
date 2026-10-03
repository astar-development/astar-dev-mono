using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;
using AStarDev.ScraperPlaying.Startup;
using AStarDev.ScraperPlaying.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Tags;

/// <summary>Runs the real tag catalogue, tags query and ignored-wallpaper store against a temp SQLite file, asserting on what ends up stored.</summary>
public sealed class GivenATagCatalogueOverARealDatabase : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-tag-catalogue-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenATagCatalogueOverARealDatabase()
    {
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<ControlDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task when_an_ignore_flag_is_removed_then_the_flag_is_saved_and_the_ignored_wallpapers_are_forgotten()
    {
        await StoreAsync(ignoredTag: true, "first", "second");

        var result = await CreateCatalogue(serviceProvider).SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(false, false, false) }, TestContext.Current.CancellationToken);

        (result.Match(_ => true, _ => false), await ReadIgnoreFlagAsync(), await CountIgnoredAsync()).ShouldBe((true, false, 0));
    }

    [Fact]
    public async Task when_an_ignore_flag_is_added_then_the_flag_is_saved_and_the_ignored_wallpapers_are_kept()
    {
        await StoreAsync(ignoredTag: false, "first", "second");

        var result = await CreateCatalogue(serviceProvider).SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(true, false, false) }, TestContext.Current.CancellationToken);

        (result.Match(_ => true, _ => false), await ReadIgnoreFlagAsync(), await CountIgnoredAsync()).ShouldBe((true, true, 2));
    }

    [Fact]
    public async Task when_forgetting_the_ignored_wallpapers_fails_then_the_flag_change_is_rolled_back()
    {
        await StoreAsync(ignoredTag: true, "first", "second");
        var failure = new InvalidOperationException("forget failed");
        using var failingProvider = new ServiceCollection().AddDataServices(databasePath).AddScoped<IIgnoredWallpapers>(_ => new FailingIgnoredWallpapers(failure)).BuildServiceProvider();

        var result = await CreateCatalogue(failingProvider).SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(false, false, false) }, TestContext.Current.CancellationToken);

        (result.Match(_ => (Exception?)null, exception => exception), await ReadIgnoreFlagAsync(), await CountIgnoredAsync()).ShouldBe((failure, true, 2));
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }

    private static TagCatalogue CreateCatalogue(ServiceProvider provider) => new(new ScopedRunner(provider.GetRequiredService<IServiceScopeFactory>()));

    private async Task StoreAsync(bool ignoredTag, params string[] ignoredHandles)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
        _ = await context.Tags.AddAsync(new TagEntity { WallhavenTagId = 1, Name = "cats", IgnoreImage = ignoredTag }, TestContext.Current.CancellationToken);
        await context.IgnoredWallpapers.AddRangeAsync(ignoredHandles.Select(handle => new IgnoredWallpaperEntity { FileHandle = FileHandle.Create(handle) }), TestContext.Current.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<bool> ReadIgnoreFlagAsync()
    {
        using var scope = serviceProvider.CreateScope();

        return (await scope.ServiceProvider.GetRequiredService<ControlDbContext>().Tags.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).IgnoreImage;
    }

    private async Task<int> CountIgnoredAsync()
    {
        using var scope = serviceProvider.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ControlDbContext>().IgnoredWallpapers.AsNoTracking().CountAsync(TestContext.Current.CancellationToken);
    }

    private sealed class FailingIgnoredWallpapers(Exception failure) : IIgnoredWallpapers
    {
        public Exceptional<Unit> Record(FileHandle fileHandle) => Unit.Instance;

        public Task<Exceptional<int>> ForgetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<Exceptional<int>>(failure);
    }
}
