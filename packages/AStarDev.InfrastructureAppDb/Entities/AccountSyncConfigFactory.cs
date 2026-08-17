using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Factory for creating instances of <see cref="AccountSyncConfig"/> with default or specified values.
/// </summary>
public static class AccountSyncConfigFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="AccountSyncConfig"/> with the provided conflict resolution policy and local sync path.
    /// </summary>
    /// <param name="policy">The conflict resolution policy.</param>
    /// <param name="path">The local sync path.</param>
    /// <returns>The created <see cref="AccountSyncConfig"/>.</returns>
    public static AccountSyncConfig Create(ConflictPolicy policy, LocalSyncPath path) => new(policy, path);

    /// <summary>
    /// Provides a default instance of <see cref="AccountSyncConfig"/> with the ignore conflict policy and an empty local sync path.
    /// </summary>
    public static AccountSyncConfig Default => new(ConflictPolicy.Ignore, LocalSyncPath.Restore(string.Empty));
}
