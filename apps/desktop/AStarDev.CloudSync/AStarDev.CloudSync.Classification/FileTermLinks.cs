using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Classification;

/// <summary>The terms that now apply to one file, for a batch replace.</summary>
public sealed record FileTermLinks
{
    internal FileTermLinks(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds)
    {
        AccountId = accountId;
        Path = path;
        TermIds = termIds;
    }

    /// <summary>The account.</summary>
    public AccountId AccountId { get; }

    /// <summary>The path below the sync root.</summary>
    public RelativeSyncPath Path { get; }

    /// <summary>The terms that apply.</summary>
    public IReadOnlyList<TermId> TermIds { get; }
}
