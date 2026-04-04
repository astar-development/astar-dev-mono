using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDriveSync.Features.Conflicts;

/// <summary>
///     EF Core + SQLite implementation of <see cref="IConflictStore"/> (CR-05, NF-05).
///     All writes use EF transactions to ensure atomicity.
///     Registered as singleton — the queue is app-wide state.
/// </summary>
internal sealed class ConflictStore(IDbContextFactory<AppDbContext> contextFactory) : IConflictStore, IDisposable
{
    private readonly Subject<ConflictQueueChanged> _subject = new();

    /// <inheritdoc />
    public IObservable<ConflictQueueChanged> ConflictQueueChanged => _subject;

    /// <inheritdoc />
    public async Task<Result<ConflictRecord, ConflictStoreError>> AddAsync(ConflictRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        _ = context.ConflictRecords.Add(record);
        _ = await context.SaveChangesAsync(ct).ConfigureAwait(false);

        await tx.CommitAsync(ct).ConfigureAwait(false);

        var pendingCount = await context.ConflictRecords.CountAsync(row => !row.IsResolved, ct).ConfigureAwait(false);
        _subject.OnNext(new ConflictQueueChanged(record.Id, ConflictQueueChangeType.ConflictAdded, pendingCount));

        return new Result<ConflictRecord, ConflictStoreError>.Ok(record);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>> GetPendingAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var records = await context.ConflictRecords
            .AsNoTracking()
            .Where(record => !record.IsResolved)
            .OrderByDescending(record => record.DetectedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok(records);
    }

    /// <inheritdoc />
    public async Task<Result<ConflictRecord, ConflictStoreError>> ResolveAsync(Guid conflictId, ResolutionStrategy strategy, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var record = await context.ConflictRecords.FindAsync([conflictId], ct).ConfigureAwait(false);

        if (record is null)

            return new Result<ConflictRecord, ConflictStoreError>.Error(ConflictStoreErrorFactory.NotFound(conflictId));

        await using var tx = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        record.IsResolved      = true;
        record.AppliedStrategy = strategy;
        _ = await context.SaveChangesAsync(ct).ConfigureAwait(false);

        await tx.CommitAsync(ct).ConfigureAwait(false);

        var pendingCount = await context.ConflictRecords.CountAsync(row => !row.IsResolved, ct).ConfigureAwait(false);
        _subject.OnNext(new ConflictQueueChanged(conflictId, ConflictQueueChangeType.ConflictResolved, pendingCount));

        return new Result<ConflictRecord, ConflictStoreError>.Ok(record);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>> GetByFilePathAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var records = await context.ConflictRecords
            .AsNoTracking()
            .Where(record => !record.IsResolved && record.FilePath == filePath)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new Result<IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok(records);
    }

    /// <inheritdoc />
    public void Dispose() => _subject.Dispose();
}
