using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;

namespace AStarDev.ControlDb.TestsUnit.TestDataFactories;

internal static class FileTagEntityFactory
{
    public static FileTagEntity CreateFileTagEntity() => new()
    {
        FileId = FileId.Empty,
        TagId = TagId.Empty
    };
}
