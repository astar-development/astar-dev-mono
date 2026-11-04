namespace AStarDev.CloudSync.TestsUnit;

internal static class TestPaths
{
    public static string Full(string unixPath) => new DirectoryInfo(unixPath).FullName;

    public static string Native(string unixPath) => unixPath.Replace('/', Path.DirectorySeparatorChar);
}
