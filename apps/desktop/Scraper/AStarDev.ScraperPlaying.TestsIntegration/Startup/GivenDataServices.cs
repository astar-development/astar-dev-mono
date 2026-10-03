using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Startup;

/// <summary>Exercises the real <see cref="DataServices.AddDataServices"/> registration against a temp SQLite file.</summary>
public sealed class GivenDataServices : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-dataservices-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenDataServices()
        => serviceProvider = new ServiceCollection()
            .AddDataServices(databasePath)
            .BuildServiceProvider();

    [Fact]
    public void when_iunitofwork_and_controldbcontext_are_resolved_in_the_same_scope_then_they_are_the_same_instance()
    {
        using var scope = serviceProvider.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var contextDirect = scope.ServiceProvider.GetRequiredService<ControlDbContext>();

        ReferenceEquals(unitOfWork, contextDirect).ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_file_tag_is_added_via_a_directly_injected_repository_then_saving_through_iunitofwork_persists_it()
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        FileId fileId;
        TagId tagId;
        using (var scope = serviceProvider.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var fileTagRepository = scope.ServiceProvider.GetRequiredService<IFileTagRepository>();

            var addedFile = unitOfWork.GetRepository<FileEntity, FileId>().Add(new FileEntity
            {
                Id = FileId.Empty,
                FileName = FileName.Create("file-name"),
                DirectoryName = DirectoryName.Create("directory-name"),
                FileHandle = FileHandle.Create("file-handle"),
                FileSize = 12345,
                FileAccessDetail = new FileAccessDetailEntity { Id = FileAccessDetailId.Empty, FileId = FileId.Empty }
            }).Match(entity => entity, exception => throw exception);

            var addedTag = unitOfWork.GetRepository<TagEntity, TagId>().Add(new TagEntity
            {
                Id = TagId.Empty,
                WallhavenTagId = 1,
                Name = "landscape"
            }).Match(entity => entity, exception => throw exception);

            fileTagRepository.Add(new FileTagEntity { FileId = addedFile.Id, TagId = addedTag.Id })
                .Match(entity => entity, exception => throw exception);

            await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

            fileId = addedFile.Id;
            tagId = addedTag.Id;
        }

        // A fresh scope proves the link was actually written to the database, not just held by a still-live
        // in-memory change tracker.
        using var verifyScope = serviceProvider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ControlDbContext>();

        var linked = await verifyContext.FileTags.SingleOrDefaultAsync(fileTag => fileTag.FileId == fileId && fileTag.TagId == tagId, TestContext.Current.CancellationToken);

        linked.ShouldNotBeNull();
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

        serviceProvider.Dispose();
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
