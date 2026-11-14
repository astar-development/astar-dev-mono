using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAnActivityLogStore : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Start = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private static ActivityEntry Downloaded(AccountId account, int minute) => ActivityEntryFactory.Create(account, Option.None<SyncPairId>(), ActivityKindFactory.CreateDownloaded(minute), Start.AddMinutes(minute));

    private static IEnumerable<ActivityEntry> Many(AccountId account, int from, int to) => Enumerable.Range(from, to - from + 1).Select(minute => Downloaded(account, minute));

    private async Task<object> AppendAsync(params ActivityEntry[] entries) => (await Database.ActivityWriter.AppendAsync(entries, CancellationToken)).Match(_ => (object)"stored", error => error);

    private async Task<IReadOnlyList<ActivityEntry>> RecentAsync(Option<AccountId> account, int count) => (await Database.ActivityReader.GetRecentAsync(account, count, CancellationToken)).Match(found => found, _ => []);

    private Task<IReadOnlyList<ActivityEntry>> RecentOfAsync(AccountId account) => RecentAsync(Option.Some(account), ActivityLogLimits.EntriesPerAccount + 10);

    [Fact]
    public async Task when_nothing_was_logged_then_the_log_is_empty()
    {
        (await RecentAsync(Option.None<AccountId>(), 10)).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_every_kind_is_appended_then_it_is_read_back_unchanged()
    {
        var pair = SyncPairIdFactory.Create(Guid.NewGuid());
        var entries = new[]
        {
            ActivityEntryFactory.Create(FirstAccount, Option.Some(pair), ActivityKindFactory.CreateUploaded(12, 84_000), Start.AddMinutes(1)),
            ActivityEntryFactory.Create(FirstAccount, Option.None<SyncPairId>(), ActivityKindFactory.CreateDownloaded(3), Start.AddMinutes(2)),
            ActivityEntryFactory.Create(FirstAccount, Option.None<SyncPairId>(), ActivityKindFactory.CreateDeleted(4), Start.AddMinutes(3)),
            ActivityEntryFactory.Create(FirstAccount, Option.None<SyncPairId>(), ActivityKindFactory.CreateConflictDetected("holiday.jpg"), Start.AddMinutes(4)),
            ActivityEntryFactory.Create(FirstAccount, Option.None<SyncPairId>(), ActivityKindFactory.CreateDetectingChanges(), Start.AddMinutes(5)),
            ActivityEntryFactory.Create(FirstAccount, Option.None<SyncPairId>(), ActivityKindFactory.CreateAuthenticated(), Start.AddMinutes(6)),
            ActivityEntryFactory.Create(FirstAccount, Option.None<SyncPairId>(), ActivityKindFactory.CreateSyncInterrupted(), Start.AddMinutes(7)),
        };

        _ = await AppendAsync(entries);

        (await RecentOfAsync(FirstAccount)).ShouldBe([.. entries.Reverse()]);
    }

    [Fact]
    public async Task when_entries_are_appended_out_of_order_then_the_newest_come_first()
    {
        _ = await AppendAsync(Downloaded(FirstAccount, 5), Downloaded(FirstAccount, 9), Downloaded(FirstAccount, 7));

        (await RecentOfAsync(FirstAccount)).Select(entry => entry.OccurredAt.Minute).ShouldBe([9, 7, 5]);
    }

    [Fact]
    public async Task when_a_count_is_given_then_only_that_many_of_the_newest_are_returned()
    {
        _ = await AppendAsync([.. Many(FirstAccount, 1, 10)]);

        (await RecentAsync(Option.Some(FirstAccount), 3)).Select(entry => entry.OccurredAt.Minute).ShouldBe([10, 9, 8]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task when_the_count_is_not_positive_then_nothing_is_returned(int count)
    {
        _ = await AppendAsync(Downloaded(FirstAccount, 1));

        (await RecentAsync(Option.None<AccountId>(), count)).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_no_account_is_given_then_every_accounts_entries_are_returned_newest_first()
    {
        _ = await AppendAsync(Downloaded(FirstAccount, 1), Downloaded(SecondAccount, 2), Downloaded(FirstAccount, 3));

        (await RecentAsync(Option.None<AccountId>(), 10)).Select(entry => (entry.AccountId, entry.OccurredAt.Minute)).ShouldBe([(FirstAccount, 3), (SecondAccount, 2), (FirstAccount, 1)]);
    }

    [Fact]
    public async Task when_an_account_is_given_then_only_its_entries_are_returned()
    {
        _ = await AppendAsync(Downloaded(FirstAccount, 1), Downloaded(SecondAccount, 2));

        (await RecentOfAsync(SecondAccount)).Select(entry => entry.AccountId).ShouldBe([SecondAccount]);
    }

    [Fact]
    public async Task when_an_empty_list_is_appended_then_it_succeeds_and_stores_nothing()
    {
        (await AppendAsync()).ShouldBe("stored");

        (await RecentAsync(Option.None<AccountId>(), 10)).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_more_than_the_cap_are_appended_over_time_then_only_the_newest_thousand_remain()
    {
        _ = await AppendAsync([.. Many(FirstAccount, 1, 1000)]);

        _ = await AppendAsync([.. Many(FirstAccount, 1001, 1005)]);

        var kept = await RecentOfAsync(FirstAccount);
        kept.Count.ShouldBe(ActivityLogLimits.EntriesPerAccount);
        kept[0].OccurredAt.ShouldBe(Start.AddMinutes(1005));
        kept[^1].OccurredAt.ShouldBe(Start.AddMinutes(6));
    }

    [Fact]
    public async Task when_exactly_the_cap_is_appended_then_nothing_is_dropped()
    {
        _ = await AppendAsync([.. Many(FirstAccount, 1, 1000)]);

        (await RecentOfAsync(FirstAccount)).Count.ShouldBe(1000);
    }

    [Fact]
    public async Task when_one_call_appends_more_than_the_cap_then_the_last_thousand_of_that_call_remain()
    {
        _ = await AppendAsync([.. Many(FirstAccount, 1, 1500)]);

        var kept = await RecentOfAsync(FirstAccount);
        kept.Count.ShouldBe(1000);
        kept[^1].OccurredAt.ShouldBe(Start.AddMinutes(501));
    }

    [Fact]
    public async Task when_one_account_exceeds_the_cap_then_another_accounts_entries_are_untouched()
    {
        _ = await AppendAsync(Downloaded(SecondAccount, 1));

        _ = await AppendAsync([.. Many(FirstAccount, 1, 1100)]);

        (await RecentOfAsync(SecondAccount)).ShouldHaveSingleItem();
        (await RecentOfAsync(FirstAccount)).Count.ShouldBe(1000);
    }

    [Fact]
    public async Task when_many_appends_race_on_one_account_then_the_cap_holds()
    {
        var writers = Enumerable.Range(0, 20).Select(writer => Task.Run(() => AppendAsync([.. Many(FirstAccount, writer * 60 + 1, writer * 60 + 60)]), CancellationToken));

        var results = await Task.WhenAll(writers);

        results.ShouldAllBe(result => result.Equals("stored"));
        (await RecentOfAsync(FirstAccount)).Count.ShouldBe(1000);
    }

    [Fact]
    public async Task when_many_appends_race_on_two_accounts_then_each_keeps_every_entry_under_the_cap()
    {
        var writers = Enumerable.Range(0, 20).Select(writer => Task.Run(() => AppendAsync(Downloaded(writer % 2 == 0 ? FirstAccount : SecondAccount, writer + 1)), CancellationToken));

        _ = await Task.WhenAll(writers);

        (await RecentOfAsync(FirstAccount)).Count.ShouldBe(10);
        (await RecentOfAsync(SecondAccount)).Count.ShouldBe(10);
    }

    [Fact]
    public async Task when_the_token_is_already_cancelled_then_nothing_is_stored_and_the_call_is_cancelled()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => Database.ActivityWriter.AppendAsync([Downloaded(FirstAccount, 1)], cancelled.Token));

        (await RecentOfAsync(FirstAccount)).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_account_is_removed_then_only_its_entries_go()
    {
        _ = await AppendAsync(Downloaded(FirstAccount, 1), Downloaded(SecondAccount, 2));

        _ = await Database.ActivityParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        (await RecentOfAsync(FirstAccount)).ShouldBeEmpty();
        (await RecentOfAsync(SecondAccount)).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task when_the_log_is_unreadable_then_a_failure_is_returned_and_logged_without_the_exception_message()
    {
        await using var unmigrated = TemporarySyncDatabase.CreateUnmigrated();

        var result = await unmigrated.ActivityReader.GetRecentAsync(Option.None<AccountId>(), 5, CancellationToken);

        result.Match(_ => "read", error => error.Message).ShouldNotBe("read");
        string.Join('\n', unmigrated.Logs.Logger.Messages).ShouldNotContain("no such table");
    }
}
