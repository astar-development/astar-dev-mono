using System.Data;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the activity log in the sync tables, newest 1,000 entries per account. An append stores and trims inside one serializable transaction.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteActivityLogStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteActivityLogStore> logger) : IActivityLogReader, IActivityLogWriter, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteActivityLogStore);

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> AppendAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken) =>
        entries.Count == 0
            ? Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance))
            : Try.RunAsync(() => StoreAsync(entries, cancellationToken), cancellationToken)
                .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The activity could not be saved."));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ActivityEntry>, ErrorResponse>> GetRecentAsync(Option<AccountId> accountId, int count, CancellationToken cancellationToken) =>
        count <= 0
            ? Task.FromResult(Result.Success<IReadOnlyList<ActivityEntry>, ErrorResponse>([]))
            : Try.RunAsync(() => ReadAsync(accountId, count, cancellationToken), cancellationToken)
                .MatchAsync(Result.Success<IReadOnlyList<ActivityEntry>, ErrorResponse>, exception => Failed<IReadOnlyList<ActivityEntry>>(exception, "The activity could not be read."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's activity could not be removed."));

    private static ActivityLogEntity ToEntity(ActivityEntry entry)
    {
        var row = new ActivityLogEntity
        {
            AccountId = entry.AccountId.Value,
            SyncPairId = entry.SyncPairId.Match(pair => (Guid?)pair.Value, () => null),
            OccurredAtUtcTicks = entry.OccurredAt.UtcTicks,
        };

        switch (entry.Kind)
        {
            case ActivityKind.Uploaded uploaded:
                row.Kind = nameof(ActivityKind.Uploaded);
                row.FileCount = uploaded.FileCount;
                row.Bytes = uploaded.Bytes;
                break;
            case ActivityKind.Downloaded downloaded:
                row.Kind = nameof(ActivityKind.Downloaded);
                row.FileCount = downloaded.FileCount;
                break;
            case ActivityKind.Deleted deleted:
                row.Kind = nameof(ActivityKind.Deleted);
                row.FileCount = deleted.FileCount;
                break;
            case ActivityKind.ConflictDetected conflict:
                row.Kind = nameof(ActivityKind.ConflictDetected);
                row.FileName = conflict.FileName;
                break;
            case ActivityKind.Authenticated:
                row.Kind = nameof(ActivityKind.Authenticated);
                break;
            default:
                row.Kind = nameof(ActivityKind.DetectingChanges);
                break;
        }

        return row;
    }

    private static ActivityEntry ToEntry(ActivityLogEntity row) =>
        ActivityEntryFactory.Create(
            AccountIdFactory.Create(row.AccountId),
            row.SyncPairId is { } pair ? Option.Some(SyncPairIdFactory.Create(pair)) : Option.None<SyncPairId>(),
            ToKind(row),
            new DateTimeOffset(row.OccurredAtUtcTicks, TimeSpan.Zero));

    private static ActivityKind ToKind(ActivityLogEntity row) => row.Kind switch
    {
        nameof(ActivityKind.Uploaded) => ActivityKindFactory.CreateUploaded(row.FileCount, row.Bytes),
        nameof(ActivityKind.Downloaded) => ActivityKindFactory.CreateDownloaded(row.FileCount),
        nameof(ActivityKind.Deleted) => ActivityKindFactory.CreateDeleted(row.FileCount),
        nameof(ActivityKind.ConflictDetected) => ActivityKindFactory.CreateConflictDetected(row.FileName ?? string.Empty),
        nameof(ActivityKind.Authenticated) => ActivityKindFactory.CreateAuthenticated(),
        _ => ActivityKindFactory.CreateDetectingChanges(),
    };

    private static async Task TrimAsync(SyncDbContext dbContext, Guid accountId, CancellationToken cancellationToken)
    {
        var oldestKept = await dbContext.ActivityLog
            .Where(row => row.AccountId == accountId)
            .OrderByDescending(row => row.Id)
            .Skip(ActivityLogLimits.EntriesPerAccount - 1)
            .Select(row => (long?)row.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (oldestKept is { } id)
            _ = await dbContext.ActivityLog.Where(row => row.AccountId == accountId && row.Id < id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Unit> StoreAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        dbContext.ActivityLog.AddRange(entries.Select(ToEntity));
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var accountId in entries.Select(entry => entry.AccountId.Value).Distinct())
            await TrimAsync(dbContext, accountId, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private async Task<IReadOnlyList<ActivityEntry>> ReadAsync(Option<AccountId> accountId, int count, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var wanted = accountId.Match(account => (Guid?)account.Value, () => null);
        var rows = await dbContext.ActivityLog
            .AsNoTracking()
            .Where(row => wanted == null || row.AccountId == wanted)
            .OrderByDescending(row => row.OccurredAtUtcTicks)
            .ThenByDescending(row => row.Id)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.Select(ToEntry)];
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.ActivityLog.Where(row => row.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
