using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates a sync root folder on disk and undoes that creation again (#1394, #1512).</summary>
internal static class SyncRootFolder
{
    /// <summary>Creates the folder when it is missing and its parent exists.</summary>
    /// <param name="candidate">The folder to ensure.</param>
    /// <returns><see langword="true"/> when this call created the folder, or why it could not be created.</returns>
    internal static Result<bool, SyncRootRejection> Ensure(IDirectoryInfo? candidate)
    {
        if (candidate is null || candidate.Exists)
            return Result.Success<bool, SyncRootRejection>(false);

        if (candidate.Parent is not { Exists: true })
            return Result.Failure<bool, SyncRootRejection>(SyncRootRejectionFactory.CreateUnavailable());

        return Create(candidate);
    }

    /// <summary>Creates the folder and any missing ancestors, for a root the app proposed itself.</summary>
    /// <param name="candidate">The folder to ensure.</param>
    /// <returns><see langword="true"/> when this call created the folder, or why it could not be created.</returns>
    internal static Result<bool, SyncRootRejection> EnsureWithAncestors(IDirectoryInfo? candidate) =>
        candidate is null || candidate.Exists ? Result.Success<bool, SyncRootRejection>(false) : Create(candidate);

    /// <summary>Removes the folder when this flow created it and it is still empty.</summary>
    /// <param name="folder">The folder to remove.</param>
    /// <param name="created">Whether this flow created it.</param>
    internal static void RemoveIfCreated(IDirectoryInfo folder, bool created)
    {
        if (created)
            _ = Try.Run(() => Delete(folder));
    }

    private static Result<bool, SyncRootRejection> Create(IDirectoryInfo candidate) =>
        Try.Run(() => CreateFolder(candidate)).Match(_ => Result.Success<bool, SyncRootRejection>(true), Rejected);

    private static Result<bool, SyncRootRejection> Rejected(Exception exception) =>
        Result.Failure<bool, SyncRootRejection>(exception is UnauthorizedAccessException ? SyncRootRejectionFactory.CreateNotWritable() : SyncRootRejectionFactory.CreateUnavailable());

    private static Unit CreateFolder(IDirectoryInfo candidate)
    {
        candidate.Create();

        return Unit.Instance;
    }

    private static Unit Delete(IDirectoryInfo folder)
    {
        folder.Delete();

        return Unit.Instance;
    }
}
