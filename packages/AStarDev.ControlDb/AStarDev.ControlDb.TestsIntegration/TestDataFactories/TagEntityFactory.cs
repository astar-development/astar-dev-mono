using AStarDev.ControlDb.TagDetail;

namespace AStarDev.ControlDb.TestsIntegration.TestDataFactories;

internal static class TagEntityFactory
{
    public static TagEntity CreateTagEntity(int wallhavenTagId = 123, string name = "landscape") => new()
    {
        Id = TagId.Empty,
        WallhavenTagId = wallhavenTagId,
        Name = name,
        Alias = "landscapes",
        CategoryId = 1,
        Category = "Nature",
        Purity = "sfw"
    };
}
