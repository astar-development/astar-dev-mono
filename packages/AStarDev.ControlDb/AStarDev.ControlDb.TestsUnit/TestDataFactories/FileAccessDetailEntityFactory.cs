using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsUnit.TestDataFactories;

internal static class FileAccessDetailEntityFactory
{
    public static FileAccessDetailEntity CreateFileAccessDetailEntity()
    {
        var fileAccessDetail = new FileAccessDetailEntity
        {
            Id = FileAccessDetailId.Empty,
            FileId = FileId.Empty,
        };

        return fileAccessDetail;
    }
}