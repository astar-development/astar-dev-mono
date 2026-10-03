using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ControlDb.TagDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAFileDetailsClearer : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-file-details-clearer-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAFileDetailsClearer()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_files_exist_then_every_file_and_its_child_rows_are_removed_and_the_count_is_returned()
    {
        var first = FileEntityFactory.CreateFileEntity(FileName.Create("first"));
        var second = FileEntityFactory.CreateFileEntity(FileName.Create("second"));
        var tag = TagEntityFactory.CreateTagEntity();
        await context.Files.AddRangeAsync([first, second], TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tag, TestContext.Current.CancellationToken);
        _ = new FileTagRepository(context).Add(new FileTagEntity { FileId = first.Id, TagId = tag.Id });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new FileDetailsClearer(context).ClearAsync(TestContext.Current.CancellationToken);

        result.Match(count => count, exception => throw exception).ShouldBe(2);
        (await context.Files.CountAsync(TestContext.Current.CancellationToken),
            await context.Set<FileAccessDetailEntity>().CountAsync(TestContext.Current.CancellationToken),
            await context.Set<ImageDetailEntity>().CountAsync(TestContext.Current.CancellationToken),
            await context.Set<DeletionStatusEntity>().CountAsync(TestContext.Current.CancellationToken),
            await context.FileTags.CountAsync(TestContext.Current.CancellationToken))
            .ShouldBe((0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task when_files_are_cleared_then_the_remembered_ignored_wallpapers_are_forgotten_but_not_counted()
    {
        await context.Files.AddAsync(FileEntityFactory.CreateFileEntity(), TestContext.Current.CancellationToken);
        await context.IgnoredWallpapers.AddRangeAsync([new IgnoredWallpaperEntity { FileHandle = FileHandle.Create("first") }, new IgnoredWallpaperEntity { FileHandle = FileHandle.Create("second") }], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new FileDetailsClearer(context).ClearAsync(TestContext.Current.CancellationToken);

        (result.Match(count => count, exception => throw exception), await context.IgnoredWallpapers.CountAsync(TestContext.Current.CancellationToken)).ShouldBe((1, 0));
    }

    [Fact]
    public async Task when_the_ignored_wallpapers_cannot_be_forgotten_then_the_failure_is_returned_and_no_file_is_removed()
    {
        await context.Files.AddAsync(FileEntityFactory.CreateFileEntity(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _ = await context.Database.ExecuteSqlRawAsync("DROP TABLE IgnoredWallpaper", TestContext.Current.CancellationToken);

        var result = await new FileDetailsClearer(context).ClearAsync(TestContext.Current.CancellationToken);

        (result.Match(_ => false, _ => true), await context.Files.CountAsync(TestContext.Current.CancellationToken)).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_files_are_cleared_then_the_tags_are_left_alone()
    {
        var tag = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(FileEntityFactory.CreateFileEntity(), TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tag, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _ = await new FileDetailsClearer(context).ClearAsync(TestContext.Current.CancellationToken);

        (await context.Tags.SingleAsync(TestContext.Current.CancellationToken)).Id.ShouldBe(tag.Id);
    }

    [Fact]
    public async Task when_there_are_no_files_then_nothing_is_removed_and_zero_is_returned()
    {
        var result = await new FileDetailsClearer(context).ClearAsync(TestContext.Current.CancellationToken);

        result.Match(count => count, exception => throw exception).ShouldBe(0);
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        context.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }
}
