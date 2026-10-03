using System.Text.Json.Serialization;

namespace AStarDev.WallpaperScraper.Scraping.WallhavenResponses.DetailResponse;

public record DetailResponse([property: JsonPropertyName("data")] Data Data);
