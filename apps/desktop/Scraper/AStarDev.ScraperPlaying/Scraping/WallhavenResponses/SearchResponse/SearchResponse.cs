using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

public record SearchResponse([property: JsonPropertyName("data")] IReadOnlyList<Data> Data, [property: JsonPropertyName("meta")] Meta Meta);