using System.Data;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the app-wide automatic sync schedule in one row of the sync tables. No row means the default schedule; a row without minutes means manual only.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and saves that fail, by exception type only.</param>
internal sealed class SqliteSyncScheduleStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteSyncScheduleStore> logger) : ISyncScheduleSettings
{
    private const string Location = nameof(SqliteSyncScheduleStore);
    private const int SingleRow = 1;

    /// <inheritdoc />
    public Task<Result<SyncSchedule, ErrorResponse>> GetAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<SyncSchedule, ErrorResponse>, exception => Failed(exception, "The sync schedule could not be read."));

    /// <inheritdoc />
    public Task<Result<SyncSchedule, ErrorResponse>> SetAsync(SyncSchedule schedule, CancellationToken cancellationToken) =>
        Try.RunAsync(() => WriteAsync(schedule, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<SyncSchedule, ErrorResponse>, exception => Failed(exception, "The sync schedule could not be saved."));

    private static SyncSchedule ToSchedule(SyncScheduleEntity entity) =>
        entity.IntervalMinutes is { } minutes ? SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(minutes)) : SyncScheduleFactory.CreateManualOnly();

    private static int? ToMinutes(SyncSchedule schedule) => schedule is SyncSchedule.Every every ? (int)every.Interval.TotalMinutes : null;

    private async Task<SyncSchedule> ReadAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var row = await dbContext.SyncSchedule.AsNoTracking().Where(entity => entity.Id == SingleRow).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return row is null ? SyncScheduleFactory.CreateDefault() : ToSchedule(row);
    }

    private async Task<SyncSchedule> WriteAsync(SyncSchedule schedule, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var row = await dbContext.SyncSchedule.Where(entity => entity.Id == SingleRow).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (row is null)
            _ = dbContext.SyncSchedule.Add(new SyncScheduleEntity { Id = SingleRow, IntervalMinutes = ToMinutes(schedule) });
        else
            row.IntervalMinutes = ToMinutes(schedule);

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return schedule;
    }

    private Result<SyncSchedule, ErrorResponse> Failed(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<SyncSchedule, ErrorResponse>(new ErrorResponse(message));
    }
}
