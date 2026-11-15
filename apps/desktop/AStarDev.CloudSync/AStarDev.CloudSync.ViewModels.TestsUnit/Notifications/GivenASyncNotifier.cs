using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Notifications;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Notifications;

public sealed class GivenASyncNotifier : IDisposable
{
    private readonly InMemoryAccountStore accounts = new();
    private readonly FakeSyncActivitySource activity = new();
    private readonly ScriptedNotificationHost host = new();
    private readonly RecordingLogger<SyncNotifier> logger = new();
    private readonly FakeTimeProvider time = new();
    private readonly List<SyncNotifier> created = [];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        foreach (var notifier in created)
            notifier.Dispose();
    }

    private SyncNotifier Create()
    {
        var notifier = new SyncNotifier(activity, new PassNotificationDecider(host, host), accounts, host, TestData.CreateLocalization(), logger, time);
        created.Add(notifier);

        return notifier;
    }

    private async Task HandleAsync(SyncNotifier notifier, SyncPassFinishedEventArgs finished)
    {
        var handling = notifier.HandleAsync(finished, CancellationToken);
        while (!handling.IsCompleted)
        {
            time.Advance(SyncNotifier.CombineWindow);
            await Task.Delay(5, CancellationToken);
        }

        await handling;
    }

    private async Task AdvancePastTheWindowAsync()
    {
        await Task.Delay(100, CancellationToken);
        time.Advance(SyncNotifier.CombineWindow);
        await Task.Delay(100, CancellationToken);
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

        await HandleAsync(Create(), Finished(account));

        return host.Sent.Single().Body;
    }

    [Fact]
    public async Task when_a_pass_is_worth_a_notification_then_the_title_is_the_app_name_and_the_body_names_the_account_and_the_count()
    {
        var account = await AddAsync("Photos");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));

        await HandleAsync(Create(), Finished(account));

        (host.Sent.Single().Title, host.Sent.Single().Body).ShouldBe(("AStar Dev CloudSync", "Photos: 3 files synced."));
    }

    [Fact]
    public async Task when_the_policy_is_asked_then_it_gets_the_windows_state()
    {
        var account = await AddAsync("Photos");
        host.Current = WindowPresenceFactory.CreateInFront();

        await HandleAsync(Create(), Finished(account));

        host.PresenceAsked.ShouldBe(WindowPresenceFactory.CreateInFront());
    }

    [Fact]
    public async Task when_the_policy_says_nothing_then_nothing_is_sent()
    {
        var account = await AddAsync("Photos");

        await HandleAsync(Create(), Finished(account));

        host.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_account_is_gone_then_nothing_is_sent()
    {
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));

        await HandleAsync(Create(), Finished(AccountIdFactory.Create(Guid.NewGuid())));

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

        await HandleAsync(notifier, Finished(account));
        await HandleAsync(notifier, Finished(account));

        (host.Sent.Count, logger.Messages.Count(message => message.Contains("notification", StringComparison.OrdinalIgnoreCase))).ShouldBe((2, 1));
    }

    [Fact]
    public async Task when_a_pass_finishes_then_the_notifier_handles_it_and_after_dispose_it_does_not()
    {
        var account = await AddAsync("Photos");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        var notifier = Create();

        activity.RaiseFinished(account);
        await AdvancePastTheWindowAsync();
        var whileListening = host.Sent.Count;
        notifier.Dispose();
        activity.RaiseFinished(account);
        await AdvancePastTheWindowAsync();

        (whileListening, host.Sent.Count).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_two_accounts_finish_close_together_then_one_notification_counts_the_accounts_and_files()
    {
        var photos = await AddAsync("Photos");
        var work = await AddAsync("Work");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        var notifier = Create();

        var first = notifier.HandleAsync(Finished(photos), CancellationToken);
        var second = notifier.HandleAsync(Finished(work), CancellationToken);
        await AdvancePastTheWindowAsync();
        await Task.WhenAll(first, second);

        host.Sent.Select(message => message.Body).ShouldBe(["2 accounts synced, 6 files."]);
    }

    [Fact]
    public async Task when_three_accounts_finish_close_together_then_the_count_is_three()
    {
        var notifier = Create();
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(2));
        var passes = new List<Task>();
        foreach (var name in new[] { "Photos", "Work", "Family" })
            passes.Add(notifier.HandleAsync(Finished(await AddAsync(name)), CancellationToken));

        await AdvancePastTheWindowAsync();
        await Task.WhenAll(passes);

        host.Sent.Select(message => message.Body).ShouldBe(["3 accounts synced, 6 files."]);
    }

    [Fact]
    public async Task when_one_account_finishes_then_nothing_is_sent_before_the_window_and_the_single_account_text_after_it()
    {
        var photos = await AddAsync("Photos");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        var handling = Create().HandleAsync(Finished(photos), CancellationToken);
        await Task.Delay(100, CancellationToken);
        var before = host.Sent.Count;

        await AdvancePastTheWindowAsync();
        await handling;

        (before, string.Join("|", host.Sent.Select(message => message.Body))).ShouldBe((0, "Photos: 3 files synced."));
    }

    [Fact]
    public async Task when_the_same_account_finishes_twice_in_the_window_then_it_is_one_account_with_the_files_added_up()
    {
        var photos = await AddAsync("Photos");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        var notifier = Create();

        var first = notifier.HandleAsync(Finished(photos), CancellationToken);
        var second = notifier.HandleAsync(Finished(photos), CancellationToken);
        await AdvancePastTheWindowAsync();
        await Task.WhenAll(first, second);

        host.Sent.Select(message => message.Body).ShouldBe(["Photos: 6 files synced."]);
    }

    [Fact]
    public async Task when_a_conflict_is_found_while_a_sync_is_waiting_then_the_conflict_is_sent_at_once()
    {
        var photos = await AddAsync("Photos");
        var work = await AddAsync("Work");
        var notifier = Create();
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        var waiting = notifier.HandleAsync(Finished(photos), CancellationToken);
        host.Decision = Option.Some(SyncNotificationFactory.CreateConflictsFound(2));

        await notifier.HandleAsync(Finished(work), CancellationToken);

        host.Sent.Select(message => message.Body).ShouldBe(["Work: 2 conflicts need your attention."]);
        await AdvancePastTheWindowAsync();
        await waiting;
    }

    [Fact]
    public async Task when_a_second_batch_finishes_after_the_window_then_it_is_a_separate_notification()
    {
        var photos = await AddAsync("Photos");
        var work = await AddAsync("Work");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        var notifier = Create();

        await HandleAsync(notifier, Finished(photos));
        await HandleAsync(notifier, Finished(work));

        host.Sent.Select(message => message.Body).ShouldBe(["Photos: 3 files synced.", "Work: 3 files synced."]);
    }

    [Fact]
    public async Task when_a_combined_notification_cannot_be_sent_then_one_warning_is_logged_and_nothing_throws()
    {
        var photos = await AddAsync("Photos");
        var work = await AddAsync("Work");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        host.Outcome = Result.Failure<Unit, NotificationFailure>(NotificationFailureFactory.CreateSenderFailed());
        var notifier = Create();

        var first = notifier.HandleAsync(Finished(photos), CancellationToken);
        var second = notifier.HandleAsync(Finished(work), CancellationToken);
        await AdvancePastTheWindowAsync();
        await Task.WhenAll(first, second);

        logger.Messages.Count(message => message.Contains("notification", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    [Fact]
    public async Task when_one_of_two_accounts_is_gone_then_only_the_known_account_is_told_about()
    {
        var photos = await AddAsync("Photos");
        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(3));
        var notifier = Create();

        var known = notifier.HandleAsync(Finished(photos), CancellationToken);
        var gone = notifier.HandleAsync(Finished(AccountIdFactory.Create(Guid.NewGuid())), CancellationToken);
        await AdvancePastTheWindowAsync();
        await Task.WhenAll(known, gone);

        host.Sent.Select(message => message.Body).ShouldBe(["Photos: 3 files synced."]);
    }

    [Fact]
    public async Task when_accounts_finish_at_once_from_many_threads_then_every_one_is_counted_in_one_notification()
    {
        var accountIds = new List<AccountId>();
        foreach (var name in Enumerable.Range(0, 20).Select(index => $"Account{index}"))
            accountIds.Add(await AddAsync(name));

        host.Decision = Option.Some(SyncNotificationFactory.CreateSynced(1));
        var notifier = Create();

        var passes = accountIds.Select(id => Task.Run(() => notifier.HandleAsync(Finished(id), CancellationToken), CancellationToken)).ToList();
        await Task.Delay(300, CancellationToken);
        await AdvancePastTheWindowAsync();
        await Task.WhenAll(passes);

        host.Sent.Select(message => message.Body).ShouldBe(["20 accounts synced, 20 files."]);
    }
}
