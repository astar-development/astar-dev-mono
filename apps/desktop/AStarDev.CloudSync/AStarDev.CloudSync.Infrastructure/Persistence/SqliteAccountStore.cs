using System.Diagnostics;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Infrastructure.Persistence;

/// <summary>Keeps accounts in the CloudSync SQLite database.</summary>
/// <param name="dbContextFactory">Creates a database context per operation.</param>
/// <param name="logger">Reports saves and reads that fail.</param>
public sealed class SqliteAccountStore(IDbContextFactory<CloudSyncDbContext> dbContextFactory, ILogger<SqliteAccountStore> logger) : IAccountStore
{
    private const string OneDriveProviderKey = "OneDrive";
    private const int SqliteUniqueConstraintErrorCode = 2067;

    /// <inheritdoc />
    public Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken) =>
        TryAddOrRefreshAsync(registration, cancellationToken)
            .OrElseAsync(exception => IsUniqueConstraintViolation(exception) ? TryAddOrRefreshAsync(registration, cancellationToken) : Task.FromResult<Result<AccountAddOutcome, Exception>>(exception))
            .MatchAsync(outcome => outcome, PersistFailed);

    /// <inheritdoc />
    public Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken) =>
        Try.RunAsync(() => FindAsync(provider, key, cancellationToken), cancellationToken)
            .MatchAsync(
                found => found.Match(entity => ToStoredAccount(entity).Map(Option.Some), () => Result.Success<Option<StoredAccount>, ErrorResponse>(Option.None<StoredAccount>())),
                exception => ReadFailed<Option<StoredAccount>>(exception, "Could not look up the account.", "The account could not be looked up."));

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAllAsync(cancellationToken), cancellationToken)
            .MatchAsync(
                entities => entities.Aggregate(
                    Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([]),
                    (accounts, entity) => accounts.Bind(read => ToStoredAccount(entity).Map(IReadOnlyList<StoredAccount> (account) => [.. read, account]))),
                exception => ReadFailed<IReadOnlyList<StoredAccount>>(exception, "Could not read the accounts.", "The accounts could not be read."));

    private Task<Result<AccountAddOutcome, Exception>> TryAddOrRefreshAsync(AccountRegistration registration, CancellationToken cancellationToken) =>
        Try.RunAsync(() => AddOrRefreshAsync(registration, cancellationToken), cancellationToken).ToResultAsync(exception => exception);

    private async Task<AccountAddOutcome> AddOrRefreshAsync(AccountRegistration registration, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var providerKey = ToProviderKey(registration.Provider);
        var existing = await dbContext.Accounts.Where(account => account.Provider == providerKey && account.ProviderAccountKey == registration.Key.Value).AsAsyncEnumerable().FirstOrNoneAsync(cancellationToken).ConfigureAwait(false);
        var outcome = existing.Match(entity => Refresh(entity, registration), () => Add(dbContext, providerKey, registration));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return outcome;
    }

    private async Task<Option<AccountEntity>> FindAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var providerKey = ToProviderKey(provider);

        return await dbContext.Accounts.AsNoTracking().Where(account => account.Provider == providerKey && account.ProviderAccountKey == key.Value).AsAsyncEnumerable().FirstOrNoneAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<AccountEntity>> ReadAllAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await dbContext.Accounts.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private AccountAddOutcome PersistFailed(Exception exception)
    {
        LogMessage.Error(logger, "Could not save the account.", exception.ErrorType());

        return AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed());
    }

    private Result<T, ErrorResponse> ReadFailed<T>(Exception exception, string logMessage, string errorMessage)
    {
        LogMessage.Error(logger, logMessage, exception.ErrorType());

        return new ErrorResponse(errorMessage);
    }

    private static bool IsUniqueConstraintViolation(Exception exception) => exception is DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintErrorCode } };

    private static AccountAddOutcome Refresh(AccountEntity existing, AccountRegistration registration)
    {
        RefreshProfile(existing, registration);

        return AccountAddOutcomeFactory.CreateReAuthenticated(AccountIdFactory.Create(existing.Id));
    }

    private static AccountAddOutcome Add(CloudSyncDbContext dbContext, string providerKey, AccountRegistration registration)
    {
        var added = new AccountEntity { Id = Guid.NewGuid(), Provider = providerKey };
        RefreshProfile(added, registration);
        dbContext.Accounts.Add(added);

        return AccountAddOutcomeFactory.CreateAdded(AccountIdFactory.Create(added.Id));
    }

    private static void RefreshProfile(AccountEntity entity, AccountRegistration registration)
    {
        entity.ProviderAccountKey = registration.Key.Value;
        entity.DisplayName = registration.DisplayName;
        entity.Email = registration.Email;
        entity.DriveId = registration.DriveId.Value;
        entity.QuotaUsedBytes = registration.Storage.UsedBytes;
        entity.QuotaTotalBytes = registration.Storage.TotalBytes;
    }

    private static Result<StoredAccount, ErrorResponse> ToStoredAccount(AccountEntity entity) =>
        ProviderAccountKeyFactory.Create(entity.ProviderAccountKey)
            .Bind(key => DriveIdFactory.Create(entity.DriveId)
                .Bind(driveId => ToProvider(entity.Provider)
                    .Map(provider => StoredAccountFactory.Create(AccountIdFactory.Create(entity.Id), AccountRegistrationFactory.Create(key, provider, entity.DisplayName, entity.Email, driveId, StorageUsageFactory.Create(entity.QuotaUsedBytes, entity.QuotaTotalBytes))))));

    private static string ToProviderKey(CloudProvider provider) => provider switch
    {
        CloudProvider.OneDrive => OneDriveProviderKey,
        _ => throw new UnreachableException($"No storage key for provider {provider.GetType().Name}."),
    };

    private static Result<CloudProvider, ErrorResponse> ToProvider(string providerKey) => providerKey == OneDriveProviderKey
        ? CloudProviderFactory.CreateOneDrive()
        : new ErrorResponse("A stored account has an unknown provider.");
}
