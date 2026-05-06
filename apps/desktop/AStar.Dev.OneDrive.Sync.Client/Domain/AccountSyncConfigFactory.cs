namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for <see cref="AccountSyncConfig"/>.</summary>
public static class AccountSyncConfigFactory
{
    /// <summary>Creates an <see cref="AccountSyncConfig"/> from the given policy and path.</summary>
    public static AccountSyncConfig Create(ConflictPolicy policy, LocalSyncPath path) => new(policy, path);

    /// <summary>Default config: Ignore policy, empty local path (not yet configured).</summary>
    public static AccountSyncConfig Default => new(ConflictPolicy.Ignore, LocalSyncPath.Restore(string.Empty));
}
