using System.Data;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Writes an uploaded file's baseline row and remote index row in one transaction (#1401).</summary>
/// <param name="dbContextFactory">Creates the context for each write.</param>
/// <param name="logger">Reports a failed write by its exception type only.</param>
internal sealed class SqliteUploadRecorder(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteUploadRecorder> logger) : IUploadRecorder
{
    private const string Location = nameof(SqliteUploadRecorder);

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RecordAsync(AccountId accountId, SyncBaselineEntry baseline, RemoteItemSnapshot remote, CancellationToken cancellationToken) =>
        Try.RunAsync(() => WriteAsync(accountId, baseline, remote, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, Failed);

    private async Task<Unit> WriteAsync(AccountId accountId, SyncBaselineEntry baseline, RemoteItemSnapshot remote, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var pass = (await dbContext.RemoteCursors.AsNoTracking().Where(row => row.AccountId == accountId.Value).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false))?.ResyncPass;
        var stored = await SqliteRemoteIndexStore.LoadAsync(dbContext, accountId, [remote.Id.Value], cancellationToken).ConfigureAwait(false);
        _ = SqliteRemoteIndexStore.Upsert(dbContext, accountId, remote, stored, pass);
        await SqliteSyncBaselineStore.SaveChunkAsync(dbContext, accountId, [baseline], cancellationToken).ConfigureAwait(false);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<Unit, ErrorResponse> Failed(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The upload could not be recorded."));
    }
}
