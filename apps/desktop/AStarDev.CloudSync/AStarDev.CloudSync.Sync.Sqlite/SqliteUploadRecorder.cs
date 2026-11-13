using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Writes an uploaded file's baseline row and remote index row in one transaction (#1401).</summary>
/// <param name="dbContextFactory">Creates the context for each write.</param>
/// <param name="logger">Reports a failed write by its exception type only.</param>
internal sealed class SqliteUploadRecorder(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteUploadRecorder> logger) : IUploadRecorder
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RecordAsync(AccountId accountId, SyncBaselineEntry baseline, RemoteItemSnapshot remote, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{dbContextFactory}{logger}{accountId}{baseline}{remote}{cancellationToken}");
}
