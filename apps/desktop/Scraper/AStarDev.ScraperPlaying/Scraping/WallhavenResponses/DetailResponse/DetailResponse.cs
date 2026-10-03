using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

public record DetailResponse([property: JsonPropertyName("data")] Data Data);