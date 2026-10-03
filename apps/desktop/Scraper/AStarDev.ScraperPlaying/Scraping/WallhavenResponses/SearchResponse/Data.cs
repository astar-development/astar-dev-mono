using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

public record Data(
    string Id,
    [property: JsonPropertyName("dimension_x")]
    int DimensionX,
    [property: JsonPropertyName("dimension_y")]
    int DimensionY,
    [property: JsonPropertyName("file_size")]
    int FileSize,
    [property: JsonPropertyName("file_type")]
    string FileType,
    string Path
);
