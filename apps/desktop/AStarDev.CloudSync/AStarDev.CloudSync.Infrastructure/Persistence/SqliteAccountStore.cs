using System.Diagnostics;
using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
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
    private const int SqliteConstraintErrorCode = 19;

    /// <inheritdoc />
    public async Task<AccountAddOutcome> AddAsync(AccountRegistration registration, CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                return await AddOrRefreshAsync(registration, cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintErrorCode })
            {
                return await AddOrRefreshAsync(registration, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogMessage.Error(logger, "Could not save the account.", exception);

            return AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreatePersistFailed());
        }
    }

    /// <inheritdoc />
    public async Task<Result<Option<StoredAccount>, ErrorResponse>> GetByKeyAsync(CloudProvider provider, ProviderAccountKey key, CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var providerKey = ToProviderKey(provider);
            var entity = await dbContext.Accounts.AsNoTracking().SingleOrDefaultAsync(account => account.Provider == providerKey && account.ProviderAccountKey == key.Value, cancellationToken).ConfigureAwait(false);

            return entity is null
                ? Result.Success<Option<StoredAccount>, ErrorResponse>(Option.None<StoredAccount>())
                : ToStoredAccount(entity).Map(Option.Some);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogMessage.Error(logger, "Could not look up the account.", exception);

            return new ErrorResponse("The account could not be looked up.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StoredAccount>, ErrorResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            var entities = await dbContext.Accounts.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

            return entities.Aggregate(
                Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([]),
                (accounts, entity) => accounts.Bind(read => ToStoredAccount(entity).Map(IReadOnlyList<StoredAccount> (account) => [.. read, account])));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogMessage.Error(logger, "Could not read the accounts.", exception);

            return new ErrorResponse("The accounts could not be read.");
        }
    }

    private async Task<AccountAddOutcome> AddOrRefreshAsync(AccountRegistration registration, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var providerKey = ToProviderKey(registration.Provider);
        var existing = await dbContext.Accounts.SingleOrDefaultAsync(account => account.Provider == providerKey && account.ProviderAccountKey == registration.Key.Value, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            RefreshProfile(existing, registration);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return AccountAddOutcomeFactory.CreateReAuthenticated(AccountIdFactory.Create(existing.Id));
        }

        var added = new AccountEntity { Id = Guid.NewGuid(), Provider = providerKey, ProviderAccountKey = registration.Key.Value };
        RefreshProfile(added, registration);
        dbContext.Accounts.Add(added);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return AccountAddOutcomeFactory.CreateAdded(AccountIdFactory.Create(added.Id));
    }

    private static void RefreshProfile(AccountEntity entity, AccountRegistration registration)
    {
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
