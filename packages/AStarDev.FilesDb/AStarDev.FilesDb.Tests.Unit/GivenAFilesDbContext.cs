using AStarDev.FilesDb.Files;
using ImmutableDomain.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.FilesDb.Tests.Unit;

public sealed class GivenAFilesDbContext : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"files-db-context-{Guid.NewGuid():N}.db");
    private readonly FilesDbContext context;
    private bool disposed;

    public GivenAFilesDbContext()
    {
        var options = new DbContextOptionsBuilder<FilesDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new FilesDbContext(options);
    }

    [Fact]
    public void when_the_model_is_built_then_no_exception_is_thrown() => context.Model.ShouldNotBeNull();

    [Fact]
    public void when_accessed_the_files_repository_should_be_an_immutable_repository() => context.FilesRepository.ShouldBeAssignableTo<IImmutableEntityRepository<FileEntity>>();

    [Fact]
    public void when_the_database_is_created_then_a_file_entity_with_related_details_can_be_saved_and_reloaded()
    {
        context.Database.EnsureCreated();

        var fileEntity = new FileEntity
        {
            Name = new FileName("wallpaper.jpg"),
            Path = new DirectoryPath("/pictures/wallpapers"),
            Handle = new FileHandle("handle-1"),
            FileSize = 1024,
        };
        context.Set<FileEntity>().Add(fileEntity);
        context.Entry(fileEntity.DeletionStatus).State = EntityState.Detached;
        context.Entry(fileEntity.FileAccessDetail).State = EntityState.Detached;
        context.SaveChanges();

        var reloaded = context.Set<FileEntity>().Single();

        reloaded.Name.ShouldBe(fileEntity.Name);
        reloaded.Path.ShouldBe(fileEntity.Path);
        reloaded.Handle.ShouldBe(fileEntity.Handle);
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
