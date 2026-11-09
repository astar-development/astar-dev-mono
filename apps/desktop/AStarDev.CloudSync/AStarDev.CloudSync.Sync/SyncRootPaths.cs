using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

internal static class SyncRootPaths
{
    internal static string Trim(IPath path, string value) => value.TrimEnd(path.DirectorySeparatorChar, path.AltDirectorySeparatorChar);
}
