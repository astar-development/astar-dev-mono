using AStarDev.ControlDb.TagDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenATagsQuery : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-tags-query-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenATagsQuery()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_some_of_the_wallhaven_ids_exist_then_find_by_wallhaven_ids_returns_only_the_stored_tags()
    {
        await context.Tags.AddRangeAsync([TagEntityFactory.CreateTagEntity(wallhavenTagId: 1, name: "one"), TagEntityFactory.CreateTagEntity(wallhavenTagId: 2, name: "two"), TagEntityFactory.CreateTagEntity(wallhavenTagId: 3, name: "three")], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.FindByWallhavenIdsAsync([1, 3, 999], TestContext.Current.CancellationToken);

        result.Match(tags => tags.Select(tag => tag.Name).Order().ToList(), exception => throw exception).ShouldBe(["one", "three"]);
    }

    [Fact]
    public async Task when_no_wallhaven_ids_are_supplied_then_find_by_wallhaven_ids_returns_an_empty_list()
    {
        var query = new TagsQuery(context);

        var result = await query.FindByWallhavenIdsAsync([], TestContext.Current.CancellationToken);

        result.Match(tags => tags.Count, exception => throw exception).ShouldBe(0);
    }

    [Fact]
    public async Task when_tags_are_stored_then_their_flags_are_projected_in_one_query()
    {
        var flagged = TagEntityFactory.CreateTagEntity(wallhavenTagId: 10, name: "flagged");
        (flagged.IgnoreImage, flagged.IsName, flagged.IsFamous) = (true, true, true);
        await context.Tags.AddRangeAsync([flagged, TagEntityFactory.CreateTagEntity(wallhavenTagId: 11, name: "plain")], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.GetFlagsAsync(TestContext.Current.CancellationToken);

        result.Match(flags => flags.OrderBy(flag => flag.WallhavenTagId).ToList(), exception => throw exception)
            .ShouldBe([new TagFlagProjection(10, true, true, true), new TagFlagProjection(11, false, false, false)]);
    }

    [Fact]
    public async Task when_tags_are_listed_then_they_are_not_tracked()
    {
        await context.Tags.AddAsync(TagEntityFactory.CreateTagEntity(wallhavenTagId: 60, name: "untracked"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();
        var query = new TagsQuery(context);

        _ = await query.ListAsync(TestContext.Current.CancellationToken);

        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task when_tags_are_stored_then_list_returns_every_tag_ordered_by_name()
    {
        await context.Tags.AddRangeAsync([TagEntityFactory.CreateTagEntity(wallhavenTagId: 1, name: "beta"), TagEntityFactory.CreateTagEntity(wallhavenTagId: 2, name: "alpha")], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.ListAsync(TestContext.Current.CancellationToken);

        result.Match(tags => tags.Select(tag => tag.Name).ToList(), exception => throw exception).ShouldBe(["alpha", "beta"]);
    }

    [Fact]
    public async Task when_a_tag_is_stored_then_it_is_not_a_name_by_default()
    {
        await context.Tags.AddAsync(TagEntityFactory.CreateTagEntity(wallhavenTagId: 32, name: "default"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Tags.Single().IsName.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_tag_is_stored_then_it_is_not_famous_by_default()
    {
        await context.Tags.AddAsync(TagEntityFactory.CreateTagEntity(wallhavenTagId: 42, name: "default"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Tags.Single().IsFamous.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_tag_is_stored_then_it_does_not_ignore_images_by_default()
    {
        await context.Tags.AddAsync(TagEntityFactory.CreateTagEntity(wallhavenTagId: 12, name: "default"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Tags.Single().IgnoreImage.ShouldBeFalse();
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
