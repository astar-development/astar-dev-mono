using AStarDev.FilesDb.Files;
using AStarDev.Utilities;

namespace AStarDev.FilesDb.Tests.Unit.Files;

public class GivenAFileEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected() => CreateSut().ToJson().ShouldMatchApproved();

    private static FileEntity CreateSut()
    {
        var detailsLastUpdated = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var lastViewed = new DateTimeOffset(2024, 6, 2, 12, 0, 0, TimeSpan.Zero);
        var softDeleted = new DateTimeOffset(2024, 6, 3, 12, 0, 0, TimeSpan.Zero);
        var softDeletePending = new DateTimeOffset(2024, 6, 4, 12, 0, 0, TimeSpan.Zero);
        var hardDeleted = new DateTimeOffset(2024, 6, 4, 12, 0, 0, TimeSpan.Zero);
        var fileId = new FileId(Guid.Empty);
        var fileAccessDetailId = new FileAccessDetailId(Guid.Empty);
        var deletionStatusId = new DeletionStatusId(Guid.Empty);
        var imageDetailId = new ImageDetailId(Guid.Empty);
        var imageDetail = new ImageDetailEntity() { Id = imageDetailId, FileEntityId = fileId, Width = 800, Height = 600 };
        var fileName = new FileName("example.txt");
        var directoryPath = new DirectoryPath("/path/to/directory");
        var fileHandle = new FileHandle("unique-handle");
        var fileAccessDetail = new FileAccessDetailEntity() { Id = fileAccessDetailId, FileEntityId = fileId, DetailsLastUpdated = detailsLastUpdated, LastViewed = lastViewed, MoveRequired = true };
        var fileDeletionStatus = new DeletionStatusEntity() { Id = deletionStatusId, FileEntityId = fileId, SoftDeleted = softDeleted, SoftDeletePending = softDeletePending, HardDeletePending = hardDeleted };

        var fileEntity = new FileEntity() { Id = fileId, Name = fileName, Path = directoryPath, Handle = fileHandle, FileSize = 1024, FileAccessDetail = fileAccessDetail, ImageDetail = imageDetail, DeletionStatus = fileDeletionStatus };
        return fileEntity;
    }
}
