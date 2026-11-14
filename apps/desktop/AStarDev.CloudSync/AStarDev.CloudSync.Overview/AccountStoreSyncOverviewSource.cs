using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>Builds the overview from the saved accounts and their sync pairs, after making sure the database is migrated. There is no sync engine yet, so activity, conflicts and throughput are empty, and every account and pair shows no files, up to date and never synced.</summary>
/// <param name="database">Is migrated before the accounts are read, so a failed migration shows as a failed overview.</param>
/// <param name="accountStore">Reads the saved accounts.</param>
/// <param name="signInStates">Tells which accounts must sign in again.</param>
/// <param name="pairReader">Reads each account's sync pairs.</param>
/// <param name="runStatuses">Tells what each account's sync runs did.</param>
/// <param name="pauseReader">Tells which accounts the user paused.</param>
internal sealed class AccountStoreSyncOverviewSource(ICloudSyncDatabase database, IAccountStore accountStore, IAccountSignInStates signInStates, ISyncPairReader pairReader, ISyncRunStatusSource runStatuses, IAccountPauseReader pauseReader) : ISyncOverviewSource
{
    /// <inheritdoc />
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) =>
        database.MigrateAsync(cancellationToken)
            .BindAsync(_ => accountStore.GetAllAsync(cancellationToken))
            .BindAsync(accounts => BuildAsync(accounts, cancellationToken));

    private async Task<Result<SyncOverview, ErrorResponse>> BuildAsync(IReadOnlyList<StoredAccount> accounts, CancellationToken cancellationToken)
    {
        _ = (runStatuses, pauseReader);
        var pairsByAccount = new Dictionary<AccountId, IReadOnlyList<SyncPair>>();
        var failure = Option.None<ErrorResponse>();
        foreach (var account in accounts)
        {
            var read = await pairReader.GetAsync(account.Id, cancellationToken).ConfigureAwait(false);
            failure = read.Match(
                pairs =>
                {
                    pairsByAccount[account.Id] = pairs;

                    return Option.None<ErrorResponse>();
                },
                Option.Some);

            if (failure.Match(_ => true, () => false))
                break;
        }

        return failure.Match(
            error => Result.Failure<SyncOverview, ErrorResponse>(error),
            () => Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([.. accounts.Select(account => ToCloudAccount(account, pairsByAccount[account.Id].Count))], [.. accounts.SelectMany(account => pairsByAccount[account.Id])], [], [], [], 0, Option.None<DateTimeOffset>())));
    }

    private CloudAccount ToCloudAccount(StoredAccount account, int folderCount) =>
        CloudAccountFactory.Create(account.Id, account.Registration.DisplayName, account.Registration.Email, account.Registration.Provider, account.Registration.Storage, folderCount, 0, SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>(), signInStates.StateOf(account.Id));
}
