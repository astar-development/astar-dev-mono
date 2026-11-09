using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncRoot"/> values.</summary>
public static class SyncRootFactory
{
    /// <summary>Creates a sync root from a folder. The path is made full and loses any trailing separator.</summary>
    /// <param name="directory">The folder.</param>
    /// <returns>The root, or <see cref="SyncRootRejection.NotAbsolute"/> when the folder is missing or its path is not absolute.</returns>
    public static Result<SyncRoot, SyncRootRejection> Create(IDirectoryInfo? directory)
    {
        if (directory is null || !directory.FileSystem.Path.IsPathFullyQualified(directory.FullName))
            return Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateNotAbsolute());

        var path = directory.FileSystem.Path;
        var trimmed = SyncRootPaths.Trim(path, path.GetFullPath(directory.FullName));

        return Result.Success<SyncRoot, SyncRootRejection>(new SyncRoot(directory.FileSystem.DirectoryInfo.New(trimmed.Length == 0 ? path.GetPathRoot(directory.FullName) ?? string.Empty : trimmed)));
    }
}
