using AStarDev.ControlDb.Files;
using AStarDev.ControlDb.ScrapeConfiguration;
using ImmutableDomain.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.Tests.Unit;

public sealed class GivenAControlDbContext : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"files-db-context-{Guid.NewGuid():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAControlDbContext()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
    }

    [Fact]
    public void when_the_model_is_built_then_no_exception_is_thrown() => context.Model.ShouldNotBeNull();

    [Fact]
    public void when_accessed_the_files_repository_should_be_an_immutable_repository() => context.FilesRepository.ShouldBeAssignableTo<IImmutableEntityRepository<FileEntity>>();


    [Fact]
    public void when_accessed_the_scrape_configuration_repository_should_be_an_immutable_repository() => context.ScrapeConfigurationRepository.ShouldBeAssignableTo<IImmutableEntityRepository<ScrapeConfigurationEntity>>();

    [Fact]
    public void when_the_database_is_created_then_a_file_entity_with_related_details_can_be_saved_and_reloaded()
    {
        context.Database.EnsureCreated();

        var fileId = new FileId(Guid.Empty);
        var fileEntity = new FileEntity(fileId, new FileName("wallpaper.jpg"), new DirectoryPath("/pictures/wallpapers"), new FileHandle("handle-1"), 1024)
        {
            FileAccessDetail = new FileAccessDetailEntity(new FileAccessDetailId(Guid.Empty), fileId, null, null, false),
            DeletionStatus = new DeletionStatusEntity(new DeletionStatusId(Guid.Empty), fileId, null, null, null),
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
