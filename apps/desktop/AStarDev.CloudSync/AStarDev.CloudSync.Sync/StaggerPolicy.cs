using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
internal sealed class StaggerPolicy : IStaggerPolicy
{
    /// <inheritdoc />
    public IReadOnlyDictionary<AccountId, TimeSpan> OffsetsFor(IReadOnlyList<AccountId> accounts, TimeSpan interval)
    {
        var distinct = accounts.Distinct().ToList();

        return distinct.Select((account, index) => (account, offset: TimeSpan.FromTicks(interval.Ticks * index / distinct.Count))).ToDictionary(entry => entry.account, entry => entry.offset);
    }
}
