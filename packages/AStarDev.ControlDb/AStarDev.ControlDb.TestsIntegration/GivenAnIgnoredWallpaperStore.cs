using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAnIgnoredWallpaperStore : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-ignored-wallpapers-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAnIgnoredWallpaperStore()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_handle_is_recorded_then_it_is_stored_when_the_context_is_saved()
    {
        var store = new IgnoredWallpapers(context);

        var result = store.Record(FileHandle.Create("abc123"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        (await context.IgnoredWallpapers.ToListAsync(TestContext.Current.CancellationToken)).Select(ignored => ignored.FileHandle.Value).ShouldBe(["abc123"]);
    }

    [Fact]
    public async Task when_a_handle_is_recorded_only_then_nothing_is_stored_until_the_context_is_saved()
    {
        _ = new IgnoredWallpapers(context).Record(FileHandle.Create("abc123"));

        (await context.IgnoredWallpapers.AsNoTracking().CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task when_the_same_handle_is_recorded_twice_then_it_is_stored_once()
    {
        var store = new IgnoredWallpapers(context);

        _ = store.Record(FileHandle.Create("abc123"));
        _ = store.Record(FileHandle.Create("ABC123"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await context.IgnoredWallpapers.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task when_a_handle_is_recorded_again_after_the_context_is_saved_then_it_is_still_stored_once()
    {
        var store = new IgnoredWallpapers(context);
        _ = store.Record(FileHandle.Create("abc123"));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = store.Record(FileHandle.Create("ABC123"));
        _ = store.Record(FileHandle.Create("def456"));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await context.IgnoredWallpapers.ToListAsync(TestContext.Current.CancellationToken)).Select(ignored => ignored.FileHandle.Value).ShouldBe(["abc123", "def456"], ignoreOrder: true);
    }

    [Fact]
    public async Task when_forgetting_then_every_recorded_handle_is_removed_and_the_count_is_returned()
    {
        await context.IgnoredWallpapers.AddRangeAsync([new IgnoredWallpaperEntity { FileHandle = FileHandle.Create("first") }, new IgnoredWallpaperEntity { FileHandle = FileHandle.Create("second") }], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new IgnoredWallpapers(context).ForgetAllAsync(TestContext.Current.CancellationToken);

        (result.Match(count => count, exception => throw exception), await context.IgnoredWallpapers.CountAsync(TestContext.Current.CancellationToken)).ShouldBe((2, 0));
    }

    [Fact]
    public async Task when_forgetting_then_stored_files_are_left_alone()
    {
        await context.Files.AddAsync(FileEntityFactory.CreateFileEntity(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = await new IgnoredWallpapers(context).ForgetAllAsync(TestContext.Current.CancellationToken);

        (await context.Files.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed) return;

        disposed = true;

        if (!disposing) return;

        context.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }
}
