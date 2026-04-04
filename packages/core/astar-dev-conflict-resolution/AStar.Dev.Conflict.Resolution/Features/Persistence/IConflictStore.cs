using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.Conflict.Resolution.Features.Persistence;

/// <summary>
///     Persistent, durable queue of conflict records backed by SQLite (CR-05, NF-05).
///     All writes are atomic. Implementations must be singleton — the queue is app-wide state.
/// </summary>
public interface IConflictStore
{
    /// <summary>Observable that fires on the calling thread whenever the queue changes.</summary>
    IObservable<ConflictQueueChanged> ConflictQueueChanged { get; }

    /// <summary>Atomically persists a new conflict to the queue.</summary>
    Task<Result<ConflictRecord, ConflictStoreError>> AddAsync(ConflictRecord record, CancellationToken ct = default);

    /// <summary>Returns all unresolved conflicts, ordered newest first.</summary>
    Task<Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Marks a conflict as resolved with the given strategy.</summary>
    Task<Result<ConflictRecord, ConflictStoreError>> ResolveAsync(Guid conflictId, ResolutionStrategy strategy, CancellationToken ct = default);

    /// <summary>Returns all pending conflicts for a given file path (used by cascade logic).</summary>
    Task<Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>> GetByFilePathAsync(string filePath, CancellationToken ct = default);
}
