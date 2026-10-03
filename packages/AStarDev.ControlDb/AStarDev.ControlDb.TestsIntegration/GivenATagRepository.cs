using AStarDev.ControlDb.TagDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenATagRepository : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-tag-repository-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenATagRepository()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_tag_is_added_and_found_by_key_then_it_is_returned()
    {
        var tagEntity = TagEntityFactory.CreateTagEntity();
        var repository = context.GetRepository<TagEntity, TagId>();
        repository.Add(tagEntity).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var untrackedContext = new ControlDbContext(new DbContextOptionsBuilder<ControlDbContext>().UseSqlite($"Data Source={databasePath}").Options);
        var result = await untrackedContext.GetRepository<TagEntity, TagId>().TryFindAsync(tagEntity.Id, TestContext.Current.CancellationToken);

        var reloaded = result.Match(option => option, exception => throw exception).Match(entity => entity, () => null!);

        reloaded.ShouldNotBeNull();
        reloaded.WallhavenTagId.ShouldBe(tagEntity.WallhavenTagId);
        reloaded.Name.ShouldBe(tagEntity.Name);
    }

    [Fact]
    public async Task when_finding_by_a_key_that_does_not_exist_then_none_is_returned()
    {
        var repository = context.GetRepository<TagEntity, TagId>();

        var result = await repository.TryFindAsync(TagId.Create(), TestContext.Current.CancellationToken);

        var found = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);

        found.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_second_tag_with_the_same_wallhaven_id_is_added_then_saving_fails()
    {
        var repository = context.GetRepository<TagEntity, TagId>();
        repository.Add(TagEntityFactory.CreateTagEntity(wallhavenTagId: 42, name: "first")).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        repository.Add(TagEntityFactory.CreateTagEntity(wallhavenTagId: 42, name: "second")).Match(entity => entity, exception => throw exception);

        await Should.ThrowAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_a_tag_is_deleted_then_it_can_no_longer_be_found()
    {
        var repository = context.GetRepository<TagEntity, TagId>();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        repository.Add(tagEntity).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await repository.DeleteAsync(tagEntity.Id, TestContext.Current.CancellationToken)).Match(unit => unit, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.TryFindAsync(tagEntity.Id, TestContext.Current.CancellationToken);
        var found = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);

        found.ShouldBeFalse();
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
