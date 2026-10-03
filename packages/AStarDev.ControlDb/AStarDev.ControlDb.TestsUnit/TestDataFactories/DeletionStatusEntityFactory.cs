using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ControlDb.TestsUnit.TestDataFactories;

internal static class DeletionStatusEntityFactory
{
    public static DeletionStatusEntity CreateDeletionStatusEntity() => new()
    {
        Id = DeletionStatusId.Empty,
        FileId = FileId.Empty,
        SoftDeleted = DateTimeOffset.UtcNow,
        SoftDeletePending = DateTimeOffset.UtcNow.AddMinutes(1),
        HardDeletePending = DateTimeOffset.UtcNow.AddHours(2)
    };
}