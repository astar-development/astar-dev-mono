namespace AStarDev.ControlDb.FileDetail;

public static class DeletionStatusIdFactory
{
    extension(DeletionStatusId)
    {
        public static DeletionStatusId Create() => new(Guid.CreateVersion7());
        public static DeletionStatusId Empty => new(Guid.Empty);
    }
}
