using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
internal sealed class StaggerPolicy : IStaggerPolicy
{
    /// <inheritdoc />
    public IReadOnlyDictionary<AccountId, TimeSpan> OffsetsFor(IReadOnlyList<AccountId> accounts, TimeSpan interval) => throw new NotImplementedException($"{accounts}{interval}");
}
