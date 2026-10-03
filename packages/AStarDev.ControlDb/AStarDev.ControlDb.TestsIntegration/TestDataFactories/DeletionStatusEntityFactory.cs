using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsIntegration.TestDataFactories;

internal static class DeletionStatusEntityFactory
{
    public static DeletionStatusEntity CreateDeletionStatusEntity()
    {
        var fileId = FileId.Create();
        return new DeletionStatusEntity()
        {
            Id = DeletionStatusId.Create(),
            FileId = fileId,
            SoftDeleted = DateTimeOffset.UtcNow,
            SoftDeletePending = DateTimeOffset.UtcNow.AddMinutes(1),
            HardDeletePending = DateTimeOffset.UtcNow.AddHours(2)
        };
    }
}