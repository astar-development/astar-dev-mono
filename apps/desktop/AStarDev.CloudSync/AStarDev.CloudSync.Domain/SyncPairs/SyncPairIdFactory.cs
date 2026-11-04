using AStarDev.CloudSync.Core.SyncPairs;

namespace AStarDev.CloudSync.Domain.SyncPairs;

/// <summary>Creates <see cref="SyncPairId"/> values.</summary>
public static class SyncPairIdFactory
{
    /// <summary>Creates the sync pair id that wraps <paramref name="value"/>. The caller supplies the value, so this factory stays deterministic.</summary>
    /// <param name="value">The unique value, for example from <see cref="Guid.NewGuid"/>.</param>
    public static SyncPairId Create(Guid value) => new(value);
}
