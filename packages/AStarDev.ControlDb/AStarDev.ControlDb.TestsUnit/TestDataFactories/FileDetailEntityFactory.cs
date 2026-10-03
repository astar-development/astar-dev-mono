using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsUnit.TestDataFactories;

internal static class FileEntityFactory
{
    public static FileEntity CreateFileEntity() => new()
    {
        Id = FileId.Empty,
        FileName = FileName.Create("file-name"),
        DirectoryName = DirectoryName.Create("file-path"),
        FileHandle = FileHandle.Create("file-handle"),
        FileSize = 12345,
        IsImage = true,
        FileAccessDetail = FileAccessDetailEntityFactory.CreateFileAccessDetailEntity(),
        DeletionStatus = DeletionStatusEntityFactory.CreateDeletionStatusEntity(),
        FileType = "image/png"
    };
}
