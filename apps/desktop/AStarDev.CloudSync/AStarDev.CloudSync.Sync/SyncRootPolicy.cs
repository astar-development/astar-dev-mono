using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class SyncRootPolicy : ISyncRootPolicy
{
    /// <inheritdoc />
    public Result<SyncRoot, SyncRootRejection> Check(AccountId accountId, IDirectoryInfo candidate, IReadOnlyList<AccountSyncRoot> existing) =>
        SyncRootFactory.Create(candidate).Match(root => Accept(accountId, root, existing ?? []), Result.Failure<SyncRoot, SyncRootRejection>);

    private static Result<SyncRoot, SyncRootRejection> Accept(AccountId accountId, SyncRoot root, IReadOnlyList<AccountSyncRoot> existing) =>
        existing
            .Where(other => other.AccountId != accountId)
            .SelectMany(other => Clash(root, other))
            .FirstOrNone()
            .Match(Result.Failure<SyncRoot, SyncRootRejection>, () => Result.Success<SyncRoot, SyncRootRejection>(root));

    private static IEnumerable<SyncRootRejection> Clash(SyncRoot candidate, AccountSyncRoot other)
    {
        var candidatePaths = PathsOf(candidate.Directory);
        var otherPaths = PathsOf(other.Root.Directory);
        var pairs = candidatePaths.SelectMany(candidatePath => otherPaths.Select(otherPath => (Candidate: candidatePath, Other: otherPath))).ToList();

        if (pairs.Any(pair => string.Equals(pair.Candidate.Path, pair.Other.Path, StringComparison.OrdinalIgnoreCase)))
            yield return SyncRootRejectionFactory.CreateSameAs(other.AccountId);
        else if (pairs.Any(pair => IsInside(pair.Candidate, pair.Other)))
            yield return SyncRootRejectionFactory.CreateInsideOf(other.AccountId);
        else if (pairs.Any(pair => IsInside(pair.Other, pair.Candidate)))
            yield return SyncRootRejectionFactory.CreateContains(other.AccountId);
    }

    private static bool IsInside(NormalisedPath inner, NormalisedPath outer) => inner.Path.StartsWith(outer.Path + outer.Separator, StringComparison.OrdinalIgnoreCase);

    private static List<NormalisedPath> PathsOf(IDirectoryInfo directory)
    {
        var paths = new List<NormalisedPath> { Normalise(directory.FileSystem.Path, directory.FullName) };
        paths.AddRange(LinkTargetOf(directory).Select(target => Normalise(directory.FileSystem.Path, target)));

        return paths;
    }

    private static NormalisedPath Normalise(IPath path, string value) => new(SyncRootPaths.Trim(path, path.GetFullPath(value)), path.DirectorySeparatorChar);

    private static List<string> LinkTargetOf(IDirectoryInfo directory)
    {
        try
        {
            return directory.LinkTarget is null || directory.ResolveLinkTarget(true) is not { } target ? [] : [target.FullName];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private readonly record struct NormalisedPath(string Path, char Separator);
}
