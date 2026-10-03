using System.Text.Json;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAResponseModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void when_search_json_is_deserialized_then_wire_names_are_mapped()
    {
        const string json = """
            {
              "data": [{
                "id": "abc",
                "dimension_x": 1920,
                "dimension_y": 1080,
                "file_size": 100,
                "file_type": "image/jpeg",
                "path": "https://example.test/image.jpg"
              }],
              "meta": { "last_page": 1 }
            }
            """;

        var response = JsonSerializer.Deserialize<SearchResponse>(json, JsonOptions)!;

        response.Data.Single().DimensionX.ShouldBe(1920);
        response.Data.Single().DimensionY.ShouldBe(1080);
        response.Data.Single().FileSize.ShouldBe(100);
        response.Data.Single().FileType.ShouldBe("image/jpeg");
        response.Data.Single().Path.ShouldBe("https://example.test/image.jpg");
        response.Meta.LastPage.ShouldBe(1);
    }
}
