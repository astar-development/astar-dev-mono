using System.Text.Json;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Scraping;

public sealed class GivenACombinedApiResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void when_detail_json_is_deserialized_then_nested_api_data_is_preserved()
    {
        const string json = """
            {
              "data": {
                "tags": [{
                  "id": 1,
                  "name": "anime",
                  "alias": "Chinese cartoons",
                  "category_id": 1,
                  "category": "Anime & Manga",
                  "purity": "sfw"
                }]
              }
            }
            """;

        var response = JsonSerializer.Deserialize<DetailResponse>(json, JsonOptions)!;

        response.Data.Tags.Single().Name.ShouldBe("anime");
        response.Data.Tags.Single().CategoryId.ShouldBe(1);
    }
}
