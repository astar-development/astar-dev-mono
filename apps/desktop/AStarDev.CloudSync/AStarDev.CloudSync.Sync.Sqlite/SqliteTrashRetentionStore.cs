using System.Data;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the sync trash retention in one row of the sync tables. No row, or a value outside the allowed range, means the default.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and saves that fail, by exception type only.</param>
internal sealed class SqliteTrashRetentionStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteTrashRetentionStore> logger) : ITrashRetentionSettings
{
    private const string Location = nameof(SqliteTrashRetentionStore);
    private const int SingleRow = 1;

    /// <inheritdoc />
    public Task<Result<LocalTrashOptions, ErrorResponse>> GetAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<LocalTrashOptions, ErrorResponse>, exception => Failed(exception, "The trash retention could not be read."));

    /// <inheritdoc />
    public Task<Result<LocalTrashOptions, ErrorResponse>> SetAsync(LocalTrashOptions options, CancellationToken cancellationToken) =>
        Try.RunAsync(() => WriteAsync(options, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<LocalTrashOptions, ErrorResponse>, exception => Failed(exception, "The trash retention could not be saved."));

    private static int ToDays(LocalTrashOptions options) => (int)options.Retention.TotalDays;

    private async Task<LocalTrashOptions> ReadAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var row = await dbContext.TrashRetention.AsNoTracking().Where(entity => entity.Id == SingleRow).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return row is null ? LocalTrashOptionsFactory.Create() : LocalTrashOptionsFactory.CreateFromDays(row.Days);
    }

    private async Task<LocalTrashOptions> WriteAsync(LocalTrashOptions options, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var row = await dbContext.TrashRetention.Where(entity => entity.Id == SingleRow).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (row is null)
            _ = dbContext.TrashRetention.Add(new TrashRetentionEntity { Id = SingleRow, Days = ToDays(options) });
        else
            row.Days = ToDays(options);

        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return options;
    }

    private Result<LocalTrashOptions, ErrorResponse> Failed(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<LocalTrashOptions, ErrorResponse>(new ErrorResponse(message));
    }
}
