using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>What the attention-queue screen reads: the accounts, each account's open items and each account's sync pairs.</summary>
/// <param name="overview">Supplies the accounts.</param>
/// <param name="attention">Reads each account's open items.</param>
/// <param name="pairs">Names the sync pair of a held deletion set.</param>
public sealed class ConflictListSources(ISyncOverviewSource overview, ISyncAttentionReader attention, ISyncPairReader pairs)
{
    /// <summary>Supplies the accounts.</summary>
    public ISyncOverviewSource Overview { get; } = overview;

    /// <summary>Reads each account's open items.</summary>
    public ISyncAttentionReader Attention { get; } = attention;

    /// <summary>Names the sync pair of a held deletion set.</summary>
    public ISyncPairReader Pairs { get; } = pairs;
}
