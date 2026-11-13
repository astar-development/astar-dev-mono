using Testably.Abstractions.Helpers;
using Testably.Abstractions.Testing.FileSystem;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class DenyListing(string deniedPath) : IAccessControlStrategy
{
    public bool IsAccessGranted(string fullPath, IFileSystemExtensibility extensibility) => !string.Equals(fullPath.TrimEnd('/'), deniedPath, StringComparison.Ordinal);
}
