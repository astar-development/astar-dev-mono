using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Overview;

/// <summary>Builds the overview from the saved accounts and their sync pairs, after making sure the database is migrated. Activity is the newest entries of the activity log (#1564); conflicts and throughput stay empty until the overview work (#1408). An account's folder and file counts come from its sync baseline (#1565), its status and last sync come from its sync runs, a paused account that is not syncing and did not just fail shows as paused, and pairs show no files, up to date and never synced. Its last sync is the in-memory time of its last finished run; surviving a restart needs the run journal (#1407).</summary>
/// <param name="database">Is migrated before the accounts are read, so a failed migration shows as a failed overview.</param>
/// <param name="accountStore">Reads the saved accounts.</param>
/// <param name="signInStates">Tells which accounts must sign in again.</param>
/// <param name="pairReader">Reads each account's sync pairs.</param>
/// <param name="runStatuses">Tells what each account's sync runs did.</param>
/// <param name="pauseReader">Tells which accounts the user paused.</param>
/// <param name="baselineCounter">Counts each account's synced folders and files with a count query.</param>
/// <param name="activityReader">Reads the newest activity across every account.</param>
internal sealed class AccountStoreSyncOverviewSource(ICloudSyncDatabase database, IAccountStore accountStore, IAccountSignInStates signInStates, ISyncPairReader pairReader, ISyncRunStatusSource runStatuses, IAccountPauseReader pauseReader, ISyncBaselineCounter baselineCounter, IActivityLogReader activityReader) : ISyncOverviewSource
{
    /// <inheritdoc />
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) =>
        database.MigrateAsync(cancellationToken)
            .BindAsync(_ => accountStore.GetAllAsync(cancellationToken))
            .BindAsync(accounts => pauseReader.GetPausedAsync(cancellationToken).BindAsync(paused => activityReader.GetRecentAsync(Option.None<AccountId>(), OverviewActivityLimit.EntryCount, cancellationToken).BindAsync(activity => BuildAsync(accounts, paused, activity, cancellationToken))));

    private async Task<Result<SyncOverview, ErrorResponse>> BuildAsync(IReadOnlyList<StoredAccount> accounts, IReadOnlyList<AccountId> paused, IReadOnlyList<ActivityEntry> activity, CancellationToken cancellationToken)
    {
        var pairsByAccount = new Dictionary<AccountId, IReadOnlyList<SyncPair>>();
        var countsByAccount = new Dictionary<AccountId, BaselineCounts>();
        var failure = Option.None<ErrorResponse>();
        foreach (var account in accounts)
        {
            failure = await ReadAsync(account.Id, pairsByAccount, countsByAccount, cancellationToken).ConfigureAwait(false);

            if (failure.Match(_ => true, () => false))
                break;
        }

        return failure.Match(
            error => Result.Failure<SyncOverview, ErrorResponse>(error),
            () => Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([.. accounts.Select(account => ToCloudAccount(account, countsByAccount[account.Id], paused.Contains(account.Id)))], [.. accounts.SelectMany(account => pairsByAccount[account.Id])], activity, [], [], 0, Option.None<DateTimeOffset>())));
    }

    private async Task<Option<ErrorResponse>> ReadAsync(AccountId accountId, Dictionary<AccountId, IReadOnlyList<SyncPair>> pairsByAccount, Dictionary<AccountId, BaselineCounts> countsByAccount, CancellationToken cancellationToken)
    {
        var pairs = await pairReader.GetAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await pairs.Match(
            read =>
            {
                pairsByAccount[accountId] = read;

                return baselineCounter.CountAsync(accountId, cancellationToken)
                    .MatchAsync(
                        counts =>
                        {
                            countsByAccount[accountId] = counts;

                            return Option.None<ErrorResponse>();
                        },
                        Option.Some);
            },
            error => Task.FromResult(Option.Some(error))).ConfigureAwait(false);
    }

    private CloudAccount ToCloudAccount(StoredAccount account, BaselineCounts counts, bool isPaused) =>
        CloudAccountFactory.Create(account.Id, account.Registration.DisplayName, account.Registration.Email, account.Registration.Provider, account.Registration.Storage, counts.Folders, counts.Files, StatusOf(account.Id, isPaused), runStatuses.LastSyncOf(account.Id), signInStates.StateOf(account.Id));

    private SyncStatus StatusOf(AccountId accountId, bool isPaused)
    {
        var status = runStatuses.StatusOf(accountId);

        return isPaused && status is SyncStatus.UpToDate ? SyncStatusFactory.CreatePaused() : status;
    }
}
