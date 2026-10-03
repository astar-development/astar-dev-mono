namespace AStarDev.ControlDb.FileDetail;

public static class FileHandleFactory
{
    extension(FileHandle)
    {
        public static FileHandle Create(string value) => new(value);
    }
}
