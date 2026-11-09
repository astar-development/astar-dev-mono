using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <summary>The folder on disk an account syncs into: a full path with no trailing separator.</summary>
public sealed record SyncRoot
{
    internal SyncRoot(IDirectoryInfo directory)
    {
        Directory = directory;
    }

    /// <summary>The normalised directory.</summary>
    public IDirectoryInfo Directory { get; }
}
