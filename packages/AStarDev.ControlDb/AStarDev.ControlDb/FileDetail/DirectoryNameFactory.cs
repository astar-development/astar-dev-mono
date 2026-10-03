namespace AStarDev.ControlDb.FileDetail;

public static class DirectoryNameFactory
{
    extension(DirectoryName)
    {
        public static DirectoryName Create(string value) => new(value);
    }
}