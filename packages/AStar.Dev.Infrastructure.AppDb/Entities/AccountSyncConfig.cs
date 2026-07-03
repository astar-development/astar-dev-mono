using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents the synchronization configuration for a OneDrive account, including conflict resolution policy and local sync path.
/// </summary>
/// <param name="ConflictPolicy">The policy for resolving conflicts during synchronization.</param>
/// <param name="LocalSyncPath">The local path where the account will be synchronized.</param>
public sealed record AccountSyncConfig(ConflictPolicy ConflictPolicy, LocalSyncPath LocalSyncPath);
