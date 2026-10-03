using AStarDev.ControlDb.TagDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAFileTagRepository : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-file-tag-repository-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAFileTagRepository()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_file_tag_is_added_then_it_links_the_file_and_the_tag()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new FileTagRepository(context);

        repository.Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(fileTag => fileTag, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var linked = await context.FileTags.SingleAsync(TestContext.Current.CancellationToken);
        linked.FileId.ShouldBe(fileEntity.Id);
        linked.TagId.ShouldBe(tagEntity.Id);
    }

    [Fact]
    public async Task when_a_file_tag_that_was_added_is_deleted_before_saving_then_it_is_not_stored()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        var repository = new FileTagRepository(context);
        var fileTag = repository.Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(added => added, exception => throw exception);

        repository.Delete(fileTag).Match(unit => unit, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await context.FileTags.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await context.Files.CountAsync(TestContext.Current.CancellationToken), await context.Tags.CountAsync(TestContext.Current.CancellationToken)).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_a_file_tag_is_deleted_after_saving_then_the_link_is_removed_and_the_file_and_tag_remain()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        var repository = new FileTagRepository(context);
        var fileTag = repository.Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(added => added, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        repository.Delete(fileTag).Match(unit => unit, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await context.FileTags.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await context.Files.CountAsync(TestContext.Current.CancellationToken), await context.Tags.CountAsync(TestContext.Current.CancellationToken)).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_a_file_and_a_tag_are_linked_then_both_read_only_navigation_collections_are_populated_on_load()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        new FileTagRepository(context).Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(fileTag => fileTag, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        var file = await context.Files.Include(item => item.FileTags).SingleAsync(TestContext.Current.CancellationToken);
        var tag = await context.Tags.Include(item => item.FileTags).SingleAsync(TestContext.Current.CancellationToken);

        file.FileTags.Select(fileTag => fileTag.TagId).ShouldBe([tagEntity.Id]);
        tag.FileTags.Select(fileTag => fileTag.FileId).ShouldBe([fileEntity.Id]);
    }

    [Fact]
    public async Task when_the_linked_file_is_deleted_then_the_file_tag_is_also_deleted()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        new FileTagRepository(context).Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(fileTag => fileTag, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Files.Remove(fileEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await context.FileTags.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_linked_tag_is_deleted_while_still_referenced_then_saving_fails()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        new FileTagRepository(context).Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(fileTag => fileTag, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Should.Throw<InvalidOperationException>(() => context.Tags.Remove(tagEntity));
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
