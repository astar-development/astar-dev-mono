using AStarDev.ControlDb.Files;
using AStarDev.Utilities;

namespace AStarDev.ControlDb.Tests.Unit.Files;

public class GivenAFileEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
    {
        string sut = CreateSut().ToJson() + Environment.NewLine;
        sut.ShouldMatchApproved();
    }

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
        var imageDetail = new ImageDetailEntity(imageDetailId, fileId, 800, 600);
        var fileName = new FileName("example.txt");
        var directoryPath = new DirectoryPath("/path/to/directory");
        var fileHandle = new FileHandle("unique-handle");
        var fileAccessDetail = new FileAccessDetailEntity(fileAccessDetailId, fileId, detailsLastUpdated, lastViewed, true);
        var fileDeletionStatus = new DeletionStatusEntity(deletionStatusId, fileId, softDeleted, softDeletePending, hardDeleted);

        var fileEntity = new FileEntity(fileId, fileName, directoryPath, fileHandle, 1024)
        {
            FileAccessDetail = fileAccessDetail,
            ImageDetail = imageDetail,
            DeletionStatus = fileDeletionStatus,
        };
        return fileEntity;
    }
}
