using AStarDev.ScraperPlaying.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Tags;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.TestsUnit.Tags;

public sealed class GivenATagCatalogue : IDisposable
{
    private readonly FakeTagsQuery query = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeIgnoredWallpapers ignoredWallpapers = new();
    private readonly ServiceProvider serviceProvider;
    private readonly TagCatalogue catalogue;

    public GivenATagCatalogue()
    {
        serviceProvider = new ServiceCollection().AddScoped<ITagsQuery>(_ => query).AddScoped<IUnitOfWork>(_ => unitOfWork).AddScoped<IIgnoredWallpapers>(_ => ignoredWallpapers).BuildServiceProvider();
        catalogue = new TagCatalogue(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()));
    }

    [Fact]
    public async Task when_tags_exist_then_a_summary_is_listed_for_each()
    {
        query.Tags.Add(CreateTag(1, "cats", ignoreImage: true, isName: true, isFamous: true));

        var result = await catalogue.ListAsync(TestContext.Current.CancellationToken);

        result.Match(list => list, exception => throw exception).ShouldBe([new TagSummary(1, "cats", "Nature", "sfw", true, true, true)]);
    }

    [Fact]
    public async Task when_listing_fails_then_the_failure_is_returned()
    {
        var failure = new InvalidOperationException("list failed");
        query.Failure = Option.Some<Exception>(failure);

        var result = await catalogue.ListAsync(TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, exception => exception).ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_flags_are_saved_then_only_the_named_tags_change_and_the_changes_are_saved()
    {
        query.Tags.AddRange([CreateTag(1, "cats", ignoreImage: false), CreateTag(2, "dogs", ignoreImage: true), CreateTag(3, "birds", ignoreImage: false)]);

        var result = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(true, false, true), [2] = new(false, true, false) }, TestContext.Current.CancellationToken);

        (result.Match(_ => true, _ => false), string.Join(",", query.Tags.Select(tag => $"{tag.IgnoreImage}/{tag.IsName}/{tag.IsFamous}")), unitOfWork.SaveCount).ShouldBe((true, "True/False/True,False/True/False,False/False/False", 1));
    }

    [Fact]
    public async Task when_a_tag_starts_being_ignored_then_the_remembered_ignored_wallpapers_are_kept()
    {
        query.Tags.Add(CreateTag(1, "cats", ignoreImage: false));

        _ = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(true, false, false) }, TestContext.Current.CancellationToken);

        ignoredWallpapers.ForgetCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_one_tag_starts_and_another_stops_being_ignored_then_the_remembered_ignored_wallpapers_are_forgotten_once()
    {
        query.Tags.AddRange([CreateTag(1, "cats", ignoreImage: false), CreateTag(2, "dogs", ignoreImage: true)]);

        _ = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(true, false, false), [2] = new(false, false, false) }, TestContext.Current.CancellationToken);

        ignoredWallpapers.ForgetCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_ignored_wallpapers_are_forgotten_then_the_flag_save_and_the_forgetting_share_one_transaction()
    {
        query.Tags.Add(CreateTag(1, "cats", ignoreImage: true));

        _ = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(false, false, false) }, TestContext.Current.CancellationToken);

        (unitOfWork.TransactionCount, unitOfWork.SaveCount, ignoredWallpapers.ForgetCount).ShouldBe((1, 1, 1));
    }

    [Fact]
    public async Task when_a_tag_stops_being_ignored_then_the_remembered_ignored_wallpapers_are_forgotten()
    {
        query.Tags.Add(CreateTag(1, "cats", ignoreImage: true));

        _ = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(false, false, false) }, TestContext.Current.CancellationToken);

        ignoredWallpapers.ForgetCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_no_ignore_flag_changes_then_the_remembered_ignored_wallpapers_are_kept()
    {
        query.Tags.AddRange([CreateTag(1, "cats", ignoreImage: true), CreateTag(2, "dogs", ignoreImage: false)]);

        _ = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(true, true, true), [2] = new(false, true, false) }, TestContext.Current.CancellationToken);

        ignoredWallpapers.ForgetCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_saving_the_flags_fails_then_the_remembered_ignored_wallpapers_are_kept()
    {
        query.Tags.Add(CreateTag(1, "cats", ignoreImage: false));
        unitOfWork.OnSave = _ => throw new InvalidOperationException("save failed");

        _ = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(true, false, false) }, TestContext.Current.CancellationToken);

        ignoredWallpapers.ForgetCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_forgetting_the_ignored_wallpapers_fails_then_the_failure_is_returned()
    {
        var failure = new InvalidOperationException("forget failed");
        query.Tags.Add(CreateTag(1, "cats", ignoreImage: true));
        ignoredWallpapers.ForgetResult = failure;

        var result = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(false, false, false) }, TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, exception => exception).ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task when_there_are_no_changes_then_nothing_is_saved()
    {
        query.Tags.Add(CreateTag(1, "cats", ignoreImage: false));

        var result = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags>(), TestContext.Current.CancellationToken);

        (result.Match(_ => true, _ => false), unitOfWork.SaveCount).ShouldBe((true, 0));
    }

    [Fact]
    public async Task when_saving_fails_then_the_failure_is_returned()
    {
        var failure = new InvalidOperationException("save failed");
        query.Tags.Add(CreateTag(1, "cats", ignoreImage: false));
        unitOfWork.OnSave = _ => throw failure;

        var result = await catalogue.SaveFlagsAsync(new Dictionary<int, TagFlags> { [1] = new(true, false, false) }, TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, exception => exception).ShouldBeSameAs(failure);
    }

    public void Dispose() => serviceProvider.Dispose();

    private static TagEntity CreateTag(int wallhavenTagId, string name, bool ignoreImage, bool isName = false, bool isFamous = false) => new()
    {
        WallhavenTagId = wallhavenTagId,
        Name = name,
        Category = "Nature",
        Purity = "sfw",
        IgnoreImage = ignoreImage,
        IsName = isName,
        IsFamous = isFamous
    };
}
