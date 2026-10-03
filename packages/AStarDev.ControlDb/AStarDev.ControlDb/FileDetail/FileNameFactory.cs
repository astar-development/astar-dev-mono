namespace AStarDev.ControlDb.FileDetail;

public static class FileNameFactory
{
    extension(FileName)
    {
        public static FileName Create(string value) => new(value);
    }
}
