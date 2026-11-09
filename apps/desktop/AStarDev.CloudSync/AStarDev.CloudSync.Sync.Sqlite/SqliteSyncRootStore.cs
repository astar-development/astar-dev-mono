using System.Data;
using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>Keeps the accounts' sync roots in the sync tables. A save reads every root, checks the candidate and writes it inside one immediate transaction, so two crossing changes never both succeed.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="policy">Decides whether a candidate crosses another account's root.</param>
/// <param name="homeFolder">The folder an account must not sync into.</param>
/// <param name="fileSystem">Rebuilds the stored paths as folders.</param>
/// <param name="logger">Reports reads and saves that fail, by exception type only.</param>
internal sealed class SqliteSyncRootStore(IDbContextFactory<SyncDbContext> dbContextFactory, ISyncRootPolicy policy, IHomeFolder homeFolder, IFileSystem fileSystem, ILogger<SqliteSyncRootStore> logger) : ISyncRootReader, ISyncRootWriter, IAccountRemovalParticipant
{
    private const string Location = nameof(SqliteSyncRootStore);

    /// <inheritdoc />
    public Task<Result<Option<SyncRoot>, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        GetAllAsync(cancellationToken)
            .MatchAsync(
                roots => Result.Success<Option<SyncRoot>, ErrorResponse>(roots.Where(entry => entry.AccountId == accountId).Select(entry => entry.Root).FirstOrNone()),
                Result.Failure<Option<SyncRoot>, ErrorResponse>);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AccountSyncRoot>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAllAsync(cancellationToken), cancellationToken)
            .MatchAsync(
                entities => Result.Success<IReadOnlyList<AccountSyncRoot>, ErrorResponse>([.. entities.SelectMany(ToAccountSyncRoot)]),
                exception => ReadFailed<IReadOnlyList<AccountSyncRoot>>(exception, "The sync folders could not be read."));

    /// <inheritdoc />
    public Task<Result<SyncRoot, SyncRootRejection>> SetAsync(AccountId accountId, IDirectoryInfo candidate, CancellationToken cancellationToken) =>
        SyncRootFactory.Create(candidate).Match(
            root => IsReserved(root) ? Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateReserved())) : SaveAsync(accountId, root, cancellationToken),
            rejection => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(rejection)));

    /// <inheritdoc />
    public Task<Result<Unit, ErrorResponse>> RemoveAccountDataAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Try.RunAsync(() => DeleteAsync(accountId, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<Unit, ErrorResponse>, exception => ReadFailed<Unit>(exception, "The account's sync folder could not be removed."));

    private bool IsReserved(SyncRoot root) =>
        root.Directory.Parent is null
        || homeFolder.Folder.Match(home => SyncRootFactory.Create(home).Match(homeRoot => string.Equals(homeRoot.Directory.FullName, root.Directory.FullName, StringComparison.OrdinalIgnoreCase), _ => false), () => false);

    private Task<Result<SyncRoot, SyncRootRejection>> SaveAsync(AccountId accountId, SyncRoot root, CancellationToken cancellationToken) =>
        Try.RunAsync(() => CheckAndSaveAsync(accountId, root, cancellationToken), cancellationToken)
            .MatchAsync(saved => saved, exception => SaveFailed(exception));

    private async Task<Result<SyncRoot, SyncRootRejection>> CheckAndSaveAsync(AccountId accountId, SyncRoot root, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        var entities = await dbContext.SyncRoots.ToListAsync(cancellationToken).ConfigureAwait(false);
        var checkedRoot = policy.Check(accountId, root.Directory, [.. entities.SelectMany(ToAccountSyncRoot)]);

        return await checkedRoot.MatchAsync(
                async accepted =>
                {
                    _ = Upsert(dbContext, entities, accountId, accepted);
                    _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                    return Result.Success<SyncRoot, SyncRootRejection>(accepted);
                },
                rejection => Task.FromResult(Result.Failure<SyncRoot, SyncRootRejection>(rejection)))
            .ConfigureAwait(false);
    }

    private static Unit Upsert(SyncDbContext dbContext, List<SyncRootEntity> entities, AccountId accountId, SyncRoot root) =>
        entities.FirstOrNone(entity => entity.AccountId == accountId.Value)
            .Match(
                entity =>
                {
                    entity.Path = root.Directory.FullName;

                    return Unit.Instance;
                },
                () =>
                {
                    _ = dbContext.SyncRoots.Add(new SyncRootEntity { AccountId = accountId.Value, Path = root.Directory.FullName });

                    return Unit.Instance;
                });

    private async Task<List<SyncRootEntity>> ReadAllAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.SyncRoots.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Unit> DeleteAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await dbContext.SyncRoots.Where(root => root.AccountId == accountId.Value).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private IEnumerable<AccountSyncRoot> ToAccountSyncRoot(SyncRootEntity entity) =>
        SyncRootFactory.Create(fileSystem.DirectoryInfo.New(entity.Path)).Match(root => new[] { AccountSyncRootFactory.Create(AccountIdFactory.Create(entity.AccountId), root) }, _ => []);

    private Result<T, ErrorResponse> ReadFailed<T>(Exception exception, string message)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<T, ErrorResponse>(new ErrorResponse(message));
    }

    private Result<SyncRoot, SyncRootRejection> SaveFailed(Exception exception)
    {
        LogMessage.ExceptionCaught(logger, Location, exception.ErrorType());

        return Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateStore(new ErrorResponse("The sync folder could not be saved.")));
    }
}
