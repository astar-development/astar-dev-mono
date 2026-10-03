using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAFileRepository : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-file-repository-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAFileRepository()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_file_is_added_and_found_by_key_then_its_sub_entities_are_eagerly_loaded()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var repository = context.GetRepository<FileEntity, FileId>();
        repository.Add(fileEntity).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var untrackedContext = new ControlDbContext(new DbContextOptionsBuilder<ControlDbContext>().UseSqlite($"Data Source={databasePath}").Options);
        var result = await untrackedContext.GetRepository<FileEntity, FileId>().TryFindAsync(fileEntity.Id, TestContext.Current.CancellationToken);

        var reloaded = result.Match(option => option, exception => throw exception).Match(entity => entity, () => null!);

        reloaded.ShouldNotBeNull();
        reloaded.FileAccessDetail.ShouldNotBeNull();
        reloaded.ImageDetail.ShouldNotBeNull();
        reloaded.DeletionStatus.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_a_file_is_saved_then_its_last_updated_time_is_persisted()
    {
        var lastUpdated = new DateTimeOffset(2026, 9, 30, 8, 30, 15, TimeSpan.Zero);
        var fileEntity = FileEntityFactory.CreateFileEntity();
        fileEntity.LastUpdated = lastUpdated;
        _ = context.GetRepository<FileEntity, FileId>().Add(fileEntity).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var untrackedContext = new ControlDbContext(new DbContextOptionsBuilder<ControlDbContext>().UseSqlite($"Data Source={databasePath}").Options);
        var reloaded = (await untrackedContext.GetRepository<FileEntity, FileId>().TryFindAsync(fileEntity.Id, TestContext.Current.CancellationToken)).Match(option => option, exception => throw exception).Match(entity => entity, () => null!);

        reloaded.LastUpdated.ShouldBe(lastUpdated);
    }

    [Fact]
    public async Task when_finding_by_a_key_that_does_not_exist_then_none_is_returned()
    {
        var repository = context.GetRepository<FileEntity, FileId>();

        var result = await repository.TryFindAsync(FileId.Create(), TestContext.Current.CancellationToken);

        var found = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);

        found.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_file_is_deleted_then_it_can_no_longer_be_found()
    {
        var repository = context.GetRepository<FileEntity, FileId>();
        var fileEntity = FileEntityFactory.CreateFileEntity();
        repository.Add(fileEntity).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await repository.DeleteAsync(fileEntity.Id, TestContext.Current.CancellationToken)).Match(unit => unit, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.TryFindAsync(fileEntity.Id, TestContext.Current.CancellationToken);
        var found = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);

        found.ShouldBeFalse();
    }

    [Fact]
    public async Task when_deleting_by_a_key_that_does_not_exist_then_no_exception_is_thrown()
    {
        var repository = context.GetRepository<FileEntity, FileId>();

        var result = await repository.DeleteAsync(FileId.Create(), TestContext.Current.CancellationToken);

        result.Match(_ => true, exception => throw exception).ShouldBeTrue();
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
