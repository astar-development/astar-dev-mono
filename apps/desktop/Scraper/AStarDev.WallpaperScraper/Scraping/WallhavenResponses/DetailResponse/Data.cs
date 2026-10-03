using System.Text.Json.Serialization;

namespace AStarDev.WallpaperScraper.Scraping.WallhavenResponses.DetailResponse;

public record Data([property: JsonPropertyName("tags")] IReadOnlyList<Tag> Tags);
