using System.Text.Json;
using AStarDev.WallpaperScraper.Localization;

namespace AStarDev.WallpaperScraper.TestsUnit.Localization;

public sealed class GivenTheEnGbJsonFile
{
    [Fact]
    public void when_read_then_app_title_key_is_present()
    {
        var assembly = typeof(LocalizationService).Assembly;
        using var stream = assembly.GetManifestResourceStream("AStarDev.WallpaperScraper.Assets.Localization.en-GB.json");
        stream.ShouldNotBeNull();
        using var document = JsonDocument.Parse(stream);

        document.RootElement.TryGetProperty("App.Title", out _).ShouldBeTrue();
    }

    [Fact]
    public void when_read_then_scraper_status_keys_are_present()
    {
        var assembly = typeof(LocalizationService).Assembly;
        using var stream = assembly.GetManifestResourceStream("AStarDev.WallpaperScraper.Assets.Localization.en-GB.json");
        stream.ShouldNotBeNull();
        using var document = JsonDocument.Parse(stream);

        document.RootElement.TryGetProperty("Scraper.SearchCategories.Started", out _).ShouldBeTrue();
        document.RootElement.TryGetProperty("Scraper.Top.Started", out _).ShouldBeTrue();
        document.RootElement.TryGetProperty("Scraper.Subscribed.Started", out _).ShouldBeTrue();
        document.RootElement.TryGetProperty("Scraper.All.Started", out _).ShouldBeTrue();
    }
}
