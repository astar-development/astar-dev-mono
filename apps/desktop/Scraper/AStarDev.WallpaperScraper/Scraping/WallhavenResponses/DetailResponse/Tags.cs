using System.Text.Json.Serialization;

namespace AStarDev.WallpaperScraper.Scraping.WallhavenResponses.DetailResponse;

public record Tag(
    int Id,
    string Name,
    string Alias,
    [property: JsonPropertyName("category_id")] int CategoryId,
    string Category,
    string Purity,
    bool IgnoreImage = false,
    bool IsName = false,
    bool IsFamous = false
);
