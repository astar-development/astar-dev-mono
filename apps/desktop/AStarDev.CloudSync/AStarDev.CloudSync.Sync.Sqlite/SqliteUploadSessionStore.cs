using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the saved upload sessions in the sync tables.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteUploadSessionStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteUploadSessionStore> logger) : IUploadSessionStore, IAccountRemovalParticipant
{
    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> SaveAsync(AccountId accountId, IFileInfo source, UploadSession session, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{dbContextFactory}{logger}{accountId}{source}{session}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Option<UploadSession>, ErrorResponse>> GetAsync(AccountId accountId, IFileInfo source, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{source}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IFileInfo source, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{source}{cancellationToken}");

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{accountId}{cancellationToken}");
}
