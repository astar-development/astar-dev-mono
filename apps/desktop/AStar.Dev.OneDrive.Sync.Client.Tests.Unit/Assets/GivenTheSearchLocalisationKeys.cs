using System.Text.Json;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Assets;

public sealed class GivenTheSearchLocalisationKeys
{
    private static readonly string JsonPath = Path.GetFullPath(
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", "apps", "desktop", "AStar.Dev.OneDrive.Sync.Client", "Assets", "Localization", "en-GB.json"));

    private static JsonElement RootElement()
    {
        string json = File.ReadAllText(JsonPath);
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void when_read_then_search_title_key_exists() =>
        RootElement().TryGetProperty("Search.Title", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_name_label_key_exists() =>
        RootElement().TryGetProperty("Search.Name.Label", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_name_placeholder_key_exists() =>
        RootElement().TryGetProperty("Search.Name.Placeholder", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_min_size_label_key_exists() =>
        RootElement().TryGetProperty("Search.MinSize.Label", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_max_size_label_key_exists() =>
        RootElement().TryGetProperty("Search.MaxSize.Label", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_tags_label_key_exists() =>
        RootElement().TryGetProperty("Search.Tags.Label", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_duplicates_only_label_key_exists() =>
        RootElement().TryGetProperty("Search.DuplicatesOnly.Label", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_button_key_exists() =>
        RootElement().TryGetProperty("Search.Button", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_duplicate_disclaimer_key_exists() =>
        RootElement().TryGetProperty("Search.DuplicateDisclaimer", out _).ShouldBeTrue();

    [Fact]
    public void when_read_then_search_no_results_key_exists() =>
        RootElement().TryGetProperty("Search.NoResults", out _).ShouldBeTrue();
}
