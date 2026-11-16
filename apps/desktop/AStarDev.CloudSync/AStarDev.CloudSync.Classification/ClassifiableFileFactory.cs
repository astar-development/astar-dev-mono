using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassifiableFile"/> values.</summary>
public static class ClassifiableFileFactory
{
    /// <summary>Creates a file entry.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The path below the sync root.</param>
    public static ClassifiableFile Create(AccountId accountId, RelativeSyncPath path) => new(accountId, path);
}
