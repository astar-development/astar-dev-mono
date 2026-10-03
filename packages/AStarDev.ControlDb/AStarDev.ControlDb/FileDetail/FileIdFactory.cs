namespace AStarDev.ControlDb.FileDetail;

public static class FileIdFactory
{
    extension(FileId)
    {
        public static FileId Create() => new(Guid.CreateVersion7());
        public static FileId Empty => new(Guid.Empty);
    }
}
