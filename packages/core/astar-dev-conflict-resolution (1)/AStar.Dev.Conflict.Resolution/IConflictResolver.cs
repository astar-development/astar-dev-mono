namespace AStar.Dev.Conflict.Resolution;

/// <summary>
/// Service that applies user-chosen conflict resolutions and
/// cascades decisions to matching pending conflicts (CR-08).
/// </summary>
public interface IConflictResolver
{
    /// <summary>Adds a newly detected conflict to the queue.</summary>
    /// <param name="record">The conflict record to enqueue.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddConflictAsync(ConflictRecord record, CancellationToken ct = default);

    /// <summary>Returns all pending (unresolved) conflicts.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<ConflictRecord>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Returns all conflicts (both resolved and pending).</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<ConflictRecord>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Resolves one or more conflicts with the chosen policy.
    /// Cascades the decision to all other pending conflicts that
    /// share the same file path (CR-08).
    /// </summary>
    /// <param name="conflictIds">The IDs of the conflicts to resolve.</param>
    /// <param name="policy">The resolution strategy to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The IDs of all conflicts resolved (including cascaded ones).</returns>
    Task<IReadOnlyList<Guid>> ResolveAsync(IReadOnlyList<Guid> conflictIds, ConflictPolicy policy, CancellationToken ct = default);

    /// <summary>
    /// Generates the "Keep Both" renamed file name for a given original name.
    /// Format: <c>original-name-(yyyy-MM-ddTHHmmssZ).ext</c> (CR-04).
    /// </summary>
    /// <param name="originalFileName">The original file name including extension.</param>
    /// <param name="utcNow">The UTC timestamp to embed in the name.</param>
    /// <returns>The renamed file name.</returns>
    string GenerateKeepBothName(string originalFileName, DateTimeOffset utcNow);
}
