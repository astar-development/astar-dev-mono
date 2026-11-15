using System.Data;
using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the saved upload sessions in the sync tables, one row per account and local path (compared ignoring case). A save replaces inside one serializable transaction.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports reads and writes that fail, by exception type only.</param>
internal sealed class SqliteUploadSessionStore(IDbContextFactory<SyncDbContext> dbContextFactory, ILogger<SqliteUploadSessionStore> logger) : IUploadSessionStore, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteUploadSessionStore);

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> SaveAsync(AccountId accountId, IFileInfo source, UploadSession session, CancellationToken cancellationToken) =>
        Try.RunAsync(() => UpsertAsync(accountId, KeyOf(source), session, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The upload session could not be saved."));

    /// <inheritdoc />
    public Task<Result<Option<UploadSession>, ErrorResponse>> GetAsync(AccountId accountId, IFileInfo source, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(accountId, KeyOf(source), cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Option<UploadSession>, ErrorResponse>, exception => Failed<Option<UploadSession>>(exception, "The upload session could not be read."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IFileInfo source, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, KeyOf(source), cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The upload session could not be removed."));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAccountAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => Failed<Unit>(exception, "The account's upload sessions could not be removed."));

    /// <inheritdoc />
    public Task<Result<int, ErrorResponse>> RemoveExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteExpiredAsync(now, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<int, ErrorResponse>, exception => Failed<int>(exception, "The expired upload sessions could not be removed."));

    private static string KeyOf(IFileInfo source) => source.FullName.ToUpperInvariant();

    private static UploadSession ToSession(UploadSessionEntity row) =>
        UploadSessionFactory.Create(new Uri(row.Url), new DateTimeOffset(row.ExpiresAtUtcTicks, TimeSpan.Zero), row.SourceLength, new DateTime(row.SourceWriteUtcTicks, DateTimeKind.Utc), row.ConfirmedBytes, row.Target);

    private async Task<Unit> UpsertAsync(AccountId accountId, string key, UploadSession session, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var row = await dbContext.UploadSessions.FirstOrDefaultAsync(existing => existing.AccountId == accountId.Value && existing.PathKey == key, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            row = new UploadSessionEntity { AccountId = accountId.Value, PathKey = key };
            _ = dbContext.UploadSessions.Add(row);
        }

        row.Url = session.Url.AbsoluteUri;
        row.ExpiresAtUtcTicks = session.ExpiresAt.UtcTicks;
        row.SourceLength = session.SourceLength;
        row.SourceWriteUtcTicks = session.SourceWriteUtc.Ticks;
        row.ConfirmedBytes = session.ConfirmedBytes;
        row.Target = session.Target;
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private async Task<Option<UploadSession>> ReadAsync(AccountId accountId, string key, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var row = await dbContext.UploadSessions.AsNoTracking().FirstOrDefaultAsync(existing => existing.AccountId == accountId.Value && existing.PathKey == key, cancellationToken).ConfigureAwait(false);

        return row is null ? Option.None<UploadSession>() : Option.Some(ToSession(row));
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, string key, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.UploadSessions.Where(row => row.AccountId == accountId.Value && row.PathKey == key).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private async Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.UploadSessions.Where(row => row.ExpiresAtUtcTicks <= now.UtcTicks).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Unit> DeleteAccountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.UploadSessions.Where(row => row.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private Result<T, ErrorResponse> Failed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }
}
