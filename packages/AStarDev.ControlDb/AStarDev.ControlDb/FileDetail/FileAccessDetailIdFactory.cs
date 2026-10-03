namespace AStarDev.ControlDb.FileDetail;

public static class FileAccessDetailIdFactory
{
    extension(FileAccessDetailId)
    {
        public static FileAccessDetailId Create() => new(Guid.CreateVersion7());
        public static FileAccessDetailId Empty => new(Guid.Empty);
    }
}
