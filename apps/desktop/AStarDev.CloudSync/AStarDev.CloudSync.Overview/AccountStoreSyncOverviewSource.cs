using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>Builds the overview from the saved accounts, after making sure the database is migrated. There is no sync engine yet, so sync pairs, activity, conflicts and throughput are empty, and every account shows no folders or files, up to date and never synced.</summary>
/// <param name="database">Is migrated before the accounts are read, so a failed migration shows as a failed overview.</param>
/// <param name="accountStore">Reads the saved accounts.</param>
internal sealed class AccountStoreSyncOverviewSource(ICloudSyncDatabase database, IAccountStore accountStore, IAccountSignInStates signInStates) : ISyncOverviewSource
{
    /// <inheritdoc />
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) =>
        database.MigrateAsync(cancellationToken)
            .BindAsync(_ => accountStore.GetAllAsync(cancellationToken))
            .MatchAsync(accounts => Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([.. accounts.Select(ToCloudAccount)], [], [], [], [], 0, Option.None<DateTimeOffset>())), Result.Failure<SyncOverview, ErrorResponse>);

    private CloudAccount ToCloudAccount(StoredAccount account) =>
        CloudAccountFactory.Create(account.Id, account.Registration.DisplayName, account.Registration.Email, account.Registration.Provider, account.Registration.Storage, 0, 0, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>(), signInStates.StateOf(account.Id));
}
