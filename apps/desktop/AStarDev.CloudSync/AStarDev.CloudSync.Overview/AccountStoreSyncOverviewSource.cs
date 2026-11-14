using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Overview;

/// <summary>Builds the overview from the saved accounts and their sync pairs, after making sure the database is migrated. Activity is the newest entries of the activity log (#1564); conflicts and throughput stay empty until the overview work (#1408). An account's folder and file counts come from its sync baseline (#1565) and are unknown when that count cannot be read (#1592), its status comes from its sync runs and its last sync from the run journal, a paused account that is not syncing and did not just fail shows as paused, and pairs show no files, up to date and never synced. Its last sync is read from the run journal (#1610), so it survives a restart, and shows as never when the journal cannot be read.</summary>
/// <param name="database">Is migrated before the accounts are read, so a failed migration shows as a failed overview.</param>
/// <param name="accountStore">Reads the saved accounts.</param>
/// <param name="readers">The readers for sign-in states, pairs, runs, pauses, baseline counts and activity.</param>
/// <param name="logger">Reports an activity feed that could not be read, without details.</param>
internal sealed class AccountStoreSyncOverviewSource(ICloudSyncDatabase database, IAccountStore accountStore, SyncOverviewReaders readers, ILogger<AccountStoreSyncOverviewSource> logger) : ISyncOverviewSource
{
    /// <inheritdoc />
    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) =>
        database.MigrateAsync(cancellationToken)
            .BindAsync(_ => accountStore.GetAllAsync(cancellationToken))
            .BindAsync(accounts => readers.Paused.GetPausedAsync(cancellationToken).BindAsync(paused => RecentActivityAsync(cancellationToken).BindAsync(activity => BuildAsync(accounts, paused, activity, cancellationToken))));

    private Task<Result<IReadOnlyList<ActivityEntry>, ErrorResponse>> RecentActivityAsync(CancellationToken cancellationToken) =>
        readers.Activity.GetRecentAsync(Option.None<AccountId>(), OverviewActivityLimit.EntryCount, cancellationToken)
            .MatchAsync(Result.Success<IReadOnlyList<ActivityEntry>, ErrorResponse>, _ => EmptyFeed());

    private Result<IReadOnlyList<ActivityEntry>, ErrorResponse> EmptyFeed()
    {
        LogMessage.Warning(logger, nameof(AccountStoreSyncOverviewSource), "The activity feed could not be read and is shown empty.");

        return Result.Success<IReadOnlyList<ActivityEntry>, ErrorResponse>([]);
    }

    private async Task<Result<SyncOverview, ErrorResponse>> BuildAsync(IReadOnlyList<StoredAccount> accounts, IReadOnlyList<AccountId> paused, IReadOnlyList<ActivityEntry> activity, CancellationToken cancellationToken)
    {
        var pairsByAccount = new Dictionary<AccountId, IReadOnlyList<SyncPair>>();
        var countsByAccount = new Dictionary<AccountId, Option<BaselineCounts>>();
        var failure = Option.None<ErrorResponse>();
        foreach (var account in accounts)
        {
            failure = await ReadAsync(account.Id, pairsByAccount, countsByAccount, cancellationToken).ConfigureAwait(false);

            if (failure.Match(_ => true, () => false))
                break;
        }

        var lastSyncs = new Dictionary<AccountId, Option<DateTimeOffset>>();
        foreach (var account in accounts)
            lastSyncs[account.Id] = await LastSyncAsync(account.Id, cancellationToken).ConfigureAwait(false);

        return failure.Match(
            error => Result.Failure<SyncOverview, ErrorResponse>(error),
            () => Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([.. accounts.Select(account => ToCloudAccount(account, countsByAccount[account.Id], paused.Contains(account.Id), lastSyncs[account.Id]))], [.. accounts.SelectMany(account => pairsByAccount[account.Id])], activity, [], [], 0, Option.None<DateTimeOffset>())));
    }

    private async Task<Option<ErrorResponse>> ReadAsync(AccountId accountId, Dictionary<AccountId, IReadOnlyList<SyncPair>> pairsByAccount, Dictionary<AccountId, Option<BaselineCounts>> countsByAccount, CancellationToken cancellationToken)
    {
        var pairs = await readers.Pairs.GetAsync(accountId, cancellationToken).ConfigureAwait(false);

        return await pairs.Match(
            read =>
            {
                pairsByAccount[accountId] = read;

                return readers.Baseline.CountAsync(accountId, cancellationToken)
                    .MatchAsync(
                        counts =>
                        {
                            countsByAccount[accountId] = Option.Some(counts);

                            return Option.None<ErrorResponse>();
                        },
                        error => cancellationToken.IsCancellationRequested ? Option.Some(error) : DegradedCounts(accountId, countsByAccount));
            },
            error => Task.FromResult(Option.Some(error))).ConfigureAwait(false);
    }

    private Option<ErrorResponse> DegradedCounts(AccountId accountId, Dictionary<AccountId, Option<BaselineCounts>> countsByAccount)
    {
        LogMessage.Warning(logger, nameof(AccountStoreSyncOverviewSource), "An account's baseline counts could not be read and are shown as unknown.");
        countsByAccount[accountId] = Option.None<BaselineCounts>();

        return Option.None<ErrorResponse>();
    }

    private Task<Option<DateTimeOffset>> LastSyncAsync(AccountId accountId, CancellationToken cancellationToken) =>
        readers.Journal.GetLastSyncAsync(accountId, cancellationToken).MatchAsync(last => last, _ => UnknownLastSync());

    private Option<DateTimeOffset> UnknownLastSync()
    {
        LogMessage.Warning(logger, nameof(AccountStoreSyncOverviewSource), "An account's last sync could not be read and is shown as never.");

        return Option.None<DateTimeOffset>();
    }

    private CloudAccount ToCloudAccount(StoredAccount account, Option<BaselineCounts> counts, bool isPaused, Option<DateTimeOffset> lastSync) =>
        CloudAccountFactory.Create(CloudAccountIdentityFactory.Create(account.Id, account.Registration.DisplayName, account.Registration.Email, account.Registration.Provider), account.Registration.Storage, counts.Map(read => read.Folders), counts.Map(read => read.Files), StatusOf(account.Id, isPaused), lastSync, readers.SignIn.StateOf(account.Id));

    private SyncStatus StatusOf(AccountId accountId, bool isPaused)
    {
        var status = readers.Runs.StatusOf(accountId);

        return isPaused && status is SyncStatus.UpToDate ? SyncStatusFactory.CreatePaused() : status;
    }
}
