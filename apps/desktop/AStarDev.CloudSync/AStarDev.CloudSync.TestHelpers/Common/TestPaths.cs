namespace AStarDev.CloudSync.TestHelpers.Common;

public static class TestPaths
{
    public static string Full(string unixPath) => new DirectoryInfo(unixPath).FullName;

    public static string Native(string unixPath) => unixPath.Replace('/', Path.DirectorySeparatorChar);
}
