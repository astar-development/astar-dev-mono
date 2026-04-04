using AStar.Dev.Sync.Engine.Features.StateTracking;

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence;

/// <summary>
///     Persists the current <see cref="SyncAccountState"/> and resume checkpoint for one account (EH-04, EH-05, EH-06).
///     One row per account — upserted on every state transition.
/// </summary>
public sealed class SyncStateRecord
{
    /// <summary>Account identifier (mirrors <see cref="Features.Accounts.Account.Id"/> as string).</summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>Most recently persisted sync state.</summary>
    public SyncAccountState State { get; set; }

    /// <summary>
    ///     Opaque JSON blob for resume checkpoint data; <c>null</c> when no checkpoint is active.
    /// </summary>
    public string? CheckpointJson { get; set; }
}
