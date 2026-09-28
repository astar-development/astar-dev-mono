using System.Text.Json;
using AStarDev.WallpaperScraper;

namespace AStarDev.WallpaperScraper.TestsUnit.Startup;

public sealed class GivenTestCategoryDeserialization
{
    [Fact]
    public void when_flags_are_numeric_then_deserializes_values()
    {
        const string json = """
            [
              { "Id": "10193", "SearchConfigurationId": "20BDC871-2D5C-4897-9F93-4A10A9FDBF3B", "Name": "H-M C", "LastKnownImageCount": 0, "LastPageVisited": 0, "TotalPages": 6, "IncludeInSearch": 1, "IsFamous": 0, "IsInternet": 0, "CreatedAt_Ticks": 639249212329127955, "UpdatedAt_Ticks": 639249212329128646 },
              { "Id": "103364", "SearchConfigurationId": "20BDC871-2D5C-4897-9F93-4A10A9FDBF3B", "Name": "S W", "LastKnownImageCount": 0, "LastPageVisited": 0, "TotalPages": 5, "IncludeInSearch": 1, "IsFamous": 0, "IsInternet": 0, "CreatedAt_Ticks": 639249212329128939, "UpdatedAt_Ticks": 639249212329128940 }
            ]
            """;

        var categories = JsonSerializer.Deserialize<List<TestCategory>>(json);

        categories.ShouldNotBeNull();
        categories.Count.ShouldBe(2);
        categories[0].IncludeInSearch.ShouldBeTrue();
        categories[0].IsFamous.ShouldBeFalse();
        categories[0].IsInternet.ShouldBeFalse();
    }

    [Fact]
    public void when_flags_are_booleans_then_deserializes_values()
    {
        const string json = "[{\"IncludeInSearch\":true,\"IsFamous\":false,\"IsInternet\":true}]";

        var categories = JsonSerializer.Deserialize<List<TestCategory>>(json);

        categories.ShouldNotBeNull();
        categories[0].IncludeInSearch.ShouldBeTrue();
        categories[0].IsFamous.ShouldBeFalse();
        categories[0].IsInternet.ShouldBeTrue();
    }
}
