namespace AStar.Dev.Conflict.Resolution;

/// <summary>
/// Persistence abstraction for the conflict queue.
/// Implementations must survive application restarts (CR-05).
/// </summary>
public interface IConflictStore
{
    /// <summary>Loads all conflict records from persistent storage.</summary>
    Task<IReadOnlyList<ConflictRecord>> LoadAsync(CancellationToken ct = default);

    /// <summary>Saves the full set of conflict records to persistent storage.</summary>
    Task SaveAsync(IReadOnlyList<ConflictRecord> records, CancellationToken ct = default);
}
