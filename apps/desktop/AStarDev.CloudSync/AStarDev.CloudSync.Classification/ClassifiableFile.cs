using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>A file the baseline knows about, for a reclassification run.</summary>
public sealed record ClassifiableFile
{
    internal ClassifiableFile(AccountId accountId, RelativeSyncPath path)
    {
        AccountId = accountId;
        Path = path;
    }

    /// <summary>The account.</summary>
    public AccountId AccountId { get; }

    /// <summary>The path below the sync root.</summary>
    public RelativeSyncPath Path { get; }
}
