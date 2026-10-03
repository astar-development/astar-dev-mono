using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsIntegration.TestDataFactories;

internal static class FileEntityFactory
{
    public static FileEntity CreateFileEntity() => CreateFileEntity(FileName.Create("file-name"));

    public static FileEntity CreateFileEntity(FileName fileName)
    {
        var fileId = new FileId(Guid.Empty);
        var fileDetail = new FileEntity()
        {
            Id = fileId,
            FileName = fileName,
            DirectoryName = DirectoryName.Create("directory-name"),
            FileHandle = FileHandle.Create(fileName.Value == "file-name" ? "file-handle" : $"handle-{fileName.Value}"),
            FileSize = 12345,
            IsImage = true,
            FileAccessDetail = new FileAccessDetailEntity()
            {
                Id = FileAccessDetailId.Create(),
                FileId = fileId,
                DetailsLastUpdated = DateTimeOffset.UtcNow,
                LastViewed = DateTimeOffset.UtcNow,
                MoveRequired = false
            },
            ImageDetail = new ImageDetailEntity()
            {
                Id = ImageId.Create(),
                FileId = fileId,
                Width = 1920,
                Height = 1080
            },
            DeletionStatus = new DeletionStatusEntity()
            {
                Id = DeletionStatusId.Create(),
                FileId = fileId,
                SoftDeleted = DateTimeOffset.UtcNow,
                SoftDeletePending = DateTimeOffset.UtcNow.AddMinutes(1),
                HardDeletePending = DateTimeOffset.UtcNow.AddHours(2)
            }
        };

        return fileDetail;
    }
}