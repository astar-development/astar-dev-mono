using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <summary>
/// Applies a resolved conflict outcome by performing the required file-system or download operation.
/// </summary>
public interface IConflictApplier
{
    /// <summary>
    /// Applies <paramref name="outcome"/> for <paramref name="conflict"/>.
    /// Returns <see langword="true"/> on success; <see langword="false"/> when the operation could not be completed.
    /// </summary>
    Task<bool> ApplyAsync(SyncConflict conflict, ConflictOutcome outcome, string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken ct);
}
