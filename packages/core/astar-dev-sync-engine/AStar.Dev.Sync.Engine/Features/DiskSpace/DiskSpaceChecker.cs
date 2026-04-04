using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace AStar.Dev.Sync.Engine.Features.DiskSpace;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI in SyncEngineServiceExtensions.")]
internal sealed class DiskSpaceChecker(IFileSystem fileSystem) : IDiskSpaceChecker
{
    /// <inheritdoc />
    public long GetAvailableFreeSpace(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var root = fileSystem.Path.GetPathRoot(path) ?? path;

        return new DriveInfo(root).AvailableFreeSpace;
    }
}
