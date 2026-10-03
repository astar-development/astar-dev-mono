using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

public record Data([property: JsonPropertyName("tags")] IReadOnlyList<Tag> Tags);
