using AStarDev.ControlDb.TagDetail;

namespace AStarDev.ControlDb.TestsUnit.TestDataFactories;

internal static class TagEntityFactory
{
    public static TagEntity CreateTagEntity() => new()
    {
        Id = TagId.Empty,
        WallhavenTagId = 123,
        Name = "landscape",
        Alias = "landscapes",
        CategoryId = 1,
        Category = "Nature",
        Purity = "sfw"
    };
}
