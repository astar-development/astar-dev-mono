using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Spreads the accounts' sync starts evenly across the interval (SE-05, #1403).</summary>
public interface IStaggerPolicy
{
    /// <summary>Gives each account its offset: <c>interval × index / count</c>, in the order given.</summary>
    /// <param name="accounts">The accounts, in the order they were added.</param>
    /// <param name="interval">The time between two syncs of one account.</param>
    /// <returns>Each account's offset from the start of the interval. Empty when there are no accounts.</returns>
    IReadOnlyDictionary<AccountId, TimeSpan> OffsetsFor(IReadOnlyList<AccountId> accounts, TimeSpan interval);
}
