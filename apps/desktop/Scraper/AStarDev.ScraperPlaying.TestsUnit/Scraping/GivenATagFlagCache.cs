using AStarDev.ControlDb.TagDetail;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenATagFlagCache
{
    [Fact]
    public void when_built_from_flags_then_each_flag_is_reported_for_its_tag()
    {
        var cache = TagFlagCache.From([new TagFlagProjection(1, true, false, false), new TagFlagProjection(2, false, true, true)]);

        (cache.IsIgnored(1), cache.IsName(2), cache.IsFamous(2), cache.IsStored(1), cache.IsStored(3)).ShouldBe((true, true, true, true, false));
    }

    [Fact]
    public void when_a_tag_is_not_stored_then_none_of_its_flags_are_set()
    {
        var cache = TagFlagCache.Empty;

        (cache.IsIgnored(9), cache.IsName(9), cache.IsFamous(9)).ShouldBe((false, false, false));
    }

    [Fact]
    public void when_the_same_tag_is_projected_twice_then_the_first_is_kept()
    {
        var cache = TagFlagCache.From([new TagFlagProjection(1, true, false, false), new TagFlagProjection(1, false, false, false)]);

        cache.IsIgnored(1).ShouldBeTrue();
    }
}
