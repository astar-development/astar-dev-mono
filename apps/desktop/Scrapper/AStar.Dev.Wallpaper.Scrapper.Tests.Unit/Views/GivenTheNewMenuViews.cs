using AStar.Dev.Wallpaper.Scrapper.Classifications;
using AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;
using AStar.Dev.Wallpaper.Scrapper.Tags;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Views;

public sealed class GivenTheNewMenuViews
{
    [Fact]
    public void when_classifications_view_type_is_inspected_then_it_exists_in_the_correct_namespace() =>
        typeof(ClassificationsView).ShouldNotBeNull();

    [Fact]
    public void when_classifications_view_type_is_inspected_then_it_implements_idisposable() =>
        typeof(ClassificationsView).GetInterface(nameof(IDisposable)).ShouldNotBeNull();

    [Fact]
    public void when_scrape_configuration_view_type_is_inspected_then_it_exists_in_the_correct_namespace() =>
        typeof(ScrapeConfigurationView).ShouldNotBeNull();

    [Fact]
    public void when_scrape_configuration_view_type_is_inspected_then_it_implements_idisposable() =>
        typeof(ScrapeConfigurationView).GetInterface(nameof(IDisposable)).ShouldNotBeNull();

    [Fact]
    public void when_tags_view_type_is_inspected_then_it_exists_in_the_correct_namespace() =>
        typeof(TagsView).ShouldNotBeNull();

    [Fact]
    public void when_tags_view_type_is_inspected_then_it_implements_idisposable() =>
        typeof(TagsView).GetInterface(nameof(IDisposable)).ShouldNotBeNull();
}
