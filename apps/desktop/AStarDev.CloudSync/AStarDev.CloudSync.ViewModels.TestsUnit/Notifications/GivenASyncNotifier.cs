using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Notifications;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Notifications;

public sealed class GivenASyncNotifier : IDisposable
{
    private readonly InMemoryAccountStore accounts = new();
    private readonly FakeSyncActivitySource activity = new();
    private readonly ScriptedNotificationHost host = new();
    private readonly RecordingLogger<SyncNotifier> logger = new();
    private readonly List<SyncNotifier> created = [];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        foreach (var notifier in created)
            notifier.Dispose();
    }

    private SyncNotifier Create()
    {
        var notifier = new SyncNotifier(activity, host, host, accounts, host, TestData.CreateLocalization(), logger);
        created.Add(notifier);

        return notifier;
    }

    private async Task<AccountId> AddAsync(string name)
    {
        var key = ProviderAccountKeyFactory.Create($"{name}-key").Match(account => account, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), name, $"{name.ToLowerInvariant()}@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1, 2));

        return (await accounts.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }

    private static SyncPassFinishedEventArgs Finished(AccountId accountId) => new(accountId, SyncTriggerFactory.CreateScheduled(), Result.Failure<SyncPassSummary, SyncPassFailure>(SyncPassFailureFactory.CreateCancelled()));

    private async Task<string> BodyForAsync(SyncNotification notification)
    {
        var account = await AddAsync("Photos");
        host.Decision = Option.Some(notification);

        await Create().HandleAsync(Finished(account), CancellationToken);

        return host.Sent.Single().Body;
    }

    [Fact]
    public async Task when_a_pass_is_worth_a_notification_then_the_title_is_the_app_name_and_the_body_names_the_account_and_the_count()
    {
        var account = await AddAsync("Photos");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));

        await Create().HandleAsync(Finished(account), CancellationToken);

        (host.Sent.Single().Title, host.Sent.Single().Body).ShouldBe(("AStar Dev CloudSync", "Photos: 3 files synced."));
    }

    [Fact]
    public async Task when_the_policy_is_asked_then_it_gets_the_windows_state()
    {
        var account = await AddAsync("Photos");
        host.Current = WindowPresenceFactory.CreateInFront();

        await Create().HandleAsync(Finished(account), CancellationToken);

        host.PresenceAsked.ShouldBe(WindowPresenceFactory.CreateInFront());
    }

    [Fact]
    public async Task when_the_policy_says_nothing_then_nothing_is_sent()
    {
        var account = await AddAsync("Photos");

        await Create().HandleAsync(Finished(account), CancellationToken);

        host.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_account_is_gone_then_nothing_is_sent()
    {
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));

        await Create().HandleAsync(Finished(AccountIdFactory.Create(Guid.NewGuid())), CancellationToken);

        host.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_texts_are_built_then_each_kind_reads_in_plain_words_with_singular_and_plural()
    {
        var bodies = new List<string>();
        foreach (var notification in new[] { SyncNotificationFactory.CreateSynced(1), SyncNotificationFactory.CreateConflictsFound(1), SyncNotificationFactory.CreateConflictsFound(2), SyncNotificationFactory.CreateDeletionsHeld(1), SyncNotificationFactory.CreateDeletionsHeld(4), SyncNotificationFactory.CreateSignInRequired(), SyncNotificationFactory.CreateSyncFailed() })
        {
            var local = new GivenASyncNotifier();
            bodies.Add(await local.BodyForAsync(notification));
            local.Dispose();
        }

        bodies.ShouldBe(["Photos: 1 file synced.", "Photos: 1 conflict needs your attention.", "Photos: 2 conflicts need your attention.", "Photos: 1 deletion is held for you to confirm.", "Photos: 4 deletions are held for you to confirm.", "Photos: sign-in is required.", "Photos: the last sync failed."]);
    }

    [Fact]
    public async Task when_the_sender_fails_repeatedly_then_nothing_throws_and_one_warning_is_logged()
    {
        var account = await AddAsync("Photos");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        host.Outcome = Result.Failure<Unit, NotificationFailure>(NotificationFailureFactory.CreateSenderFailed());
        var notifier = Create();

        await notifier.HandleAsync(Finished(account), CancellationToken);
        await notifier.HandleAsync(Finished(account), CancellationToken);

        (host.Sent.Count, logger.Messages.Count(message => message.Contains("notification", StringComparison.OrdinalIgnoreCase))).ShouldBe((2, 1));
    }

    [Fact]
    public async Task when_a_pass_finishes_then_the_notifier_handles_it_and_after_dispose_it_does_not()
    {
        var account = await AddAsync("Photos");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        var notifier = Create();

        activity.RaiseFinished(account);
        await Task.Delay(100, CancellationToken);
        var whileListening = host.Sent.Count;
        notifier.Dispose();
        activity.RaiseFinished(account);
        await Task.Delay(100, CancellationToken);

        (whileListening, host.Sent.Count).ShouldBe((1, 1));
    }
}
