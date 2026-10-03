using AStarDev.ControlDb.TagDetail;
using AStarDev.WallpaperScraper.Scraping;

namespace AStarDev.WallpaperScraper.TestsUnit.Scraping;

public sealed class GivenATagFlagStore
{
    private readonly TagFlagStore store = new();

    [Fact]
    public void when_nothing_has_been_loaded_then_the_store_is_empty_and_not_loaded()
        => (store.IsLoaded, store.Cache.IsStored(1)).ShouldBe((false, false));

    [Fact]
    public void when_a_cache_is_loaded_then_the_store_reports_loaded_with_that_cache()
    {
        store.Load(TagFlagCache.From([new TagFlagProjection(1, true, false, false)]));

        (store.IsLoaded, store.Cache.IsIgnored(1)).ShouldBe((true, true));
    }
}
