using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAFilesQuery : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-files-query-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAFilesQuery()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_some_of_the_handles_exist_then_get_existing_handles_returns_only_the_stored_ones()
    {
        await context.Files.AddRangeAsync([FileEntityFactory.CreateFileEntity(FileName.Create("stored-1.jpg")), FileEntityFactory.CreateFileEntity(FileName.Create("stored-2.jpg"))], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new FilesQuery(context);

        var result = await query.GetExistingHandlesAsync([FileHandle.Create("handle-stored-1.jpg"), FileHandle.Create("missing"), FileHandle.Create("handle-stored-2.jpg")], TestContext.Current.CancellationToken);

        result.Match(handles => handles.Select(handle => handle.Value).Order().ToList(), exception => throw exception).ShouldBe(["handle-stored-1.jpg", "handle-stored-2.jpg"]);
    }

    [Fact]
    public async Task when_the_stored_file_name_has_a_person_prefix_then_the_file_is_still_found_by_its_handle()
    {
        var file = FileEntityFactory.CreateFileEntity(FileName.Create("Max_Verstappen_vqyxgm.jpg"));
        file.FileHandle = FileHandle.Create("vqyxgm");
        await context.Files.AddAsync(file, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new FilesQuery(context);

        var result = await query.GetExistingHandlesAsync([FileHandle.Create("vqyxgm")], TestContext.Current.CancellationToken);

        result.Match(handles => handles.Select(handle => handle.Value).ToList(), exception => throw exception).ShouldBe(["vqyxgm"]);
    }

    [Fact]
    public async Task when_a_handle_was_recorded_as_ignored_then_it_is_reported_as_existing_alongside_stored_files()
    {
        var file = FileEntityFactory.CreateFileEntity(FileName.Create("stored.jpg"));
        file.FileHandle = FileHandle.Create("stored");
        await context.Files.AddAsync(file, TestContext.Current.CancellationToken);
        await context.IgnoredWallpapers.AddAsync(new IgnoredWallpaperEntity { FileHandle = FileHandle.Create("ignoredone") }, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new FilesQuery(context);

        var result = await query.GetExistingHandlesAsync([FileHandle.Create("stored"), FileHandle.Create("ignoredone"), FileHandle.Create("missing")], TestContext.Current.CancellationToken);

        result.Match(handles => handles.Select(handle => handle.Value).Order().ToList(), exception => throw exception).ShouldBe(["ignoredone", "stored"]);
    }

    [Fact]
    public async Task when_no_handles_are_supplied_then_get_existing_handles_returns_an_empty_list()
    {
        var query = new FilesQuery(context);

        var result = await query.GetExistingHandlesAsync([], TestContext.Current.CancellationToken);

        result.Match(handles => handles.Count, exception => throw exception).ShouldBe(0);
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
