
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="SyncPairId"/> values.</summary>
public static class SyncPairIdFactory
{
    /// <summary>Creates the sync pair id that wraps <paramref name="value"/>. The caller supplies the value, so this factory stays deterministic.</summary>
    /// <param name="value">The unique value, for example from <see cref="Guid.NewGuid"/>.</param>
    public static SyncPairId Create(Guid value) => new(value);

    /// <summary>Creates the id of the pair whose top-most Include rule is at <paramref name="path"/>. The same account and path, ignoring case, always give the same id, so a pair keeps its id across restarts.</summary>
    /// <param name="accountId">The account.</param>
    /// <param name="path">The path of the pair's top-most Include rule.</param>
    public static SyncPairId CreateForRule(AccountId accountId, LocalRelativePath path) =>
        new(new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{accountId.Value:N}|{(path.Value ?? string.Empty).ToUpperInvariant()}"))[..16]));
}
