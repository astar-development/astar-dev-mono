using AStar.Dev.Wallpaper.Scraper.DTOs;
using AStar.Dev.Wallpaper.Scraper.Support;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Support;

public sealed class GivenTheTagRuleContextFactory
{
    private static readonly TagsToIgnoreCompletely IgnoreCompletely = new();
    private static readonly TagsTextToIgnore TextToIgnore = new();

    [Fact]
    public void when_creating_a_context_then_the_initial_directory_is_preserved() =>
        TagRuleContextFactory.Create("initial-directory", "famous-directory", IgnoreCompletely, TextToIgnore).InitialDirectory.ShouldBe("initial-directory");

    [Fact]
    public void when_creating_a_context_then_the_base_directory_famous_is_preserved() =>
        TagRuleContextFactory.Create("initial-directory", "famous-directory", IgnoreCompletely, TextToIgnore).BaseDirectoryFamous.ShouldBe("famous-directory");

    [Fact]
    public void when_creating_a_context_then_the_tags_to_ignore_completely_is_preserved() =>
        TagRuleContextFactory.Create("initial-directory", "famous-directory", IgnoreCompletely, TextToIgnore).TagsToIgnoreCompletely.ShouldBeSameAs(IgnoreCompletely);

    [Fact]
    public void when_creating_a_context_then_the_tags_text_to_ignore_is_preserved() =>
        TagRuleContextFactory.Create("initial-directory", "famous-directory", IgnoreCompletely, TextToIgnore).TagsTextToIgnore.ShouldBeSameAs(TextToIgnore);
}
