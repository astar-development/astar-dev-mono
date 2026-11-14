using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncAttentionStore : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly SyncPairId Pair = SyncPairIdFactory.Create(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    private static readonly DateTimeOffset Moment = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    private ISyncAttentionReader Reader => Database.Services.GetRequiredService<ISyncAttentionReader>();

    private ISyncAttentionWriter Writer => Database.Services.GetRequiredService<ISyncAttentionWriter>();

    private ISyncAttentionCloser Closer => Database.Services.GetRequiredService<ISyncAttentionCloser>();

    private IAccountRemovalParticipant Participant => Database.Services.GetServices<IAccountRemovalParticipant>().First(participant => participant.GetType().Name == "SqliteSyncAttentionStore");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken, services => services.AddCloudSync());

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    [Fact]
    public async Task when_a_conflict_is_recorded_then_it_reads_back_with_its_snapshots()
    {
        var local = SideSnapshotFactory.CreatePresent(42, Moment, Option.Some(new ContentHash("hash-1")), Option.None<ContentTag>());
        var remote = SideSnapshotFactory.CreatePresent(43, Moment.AddMinutes(1), Option.None<ContentHash>(), Option.Some(new ContentTag("tag-1")));
        var id = new ConflictId(Guid.NewGuid());
        _ = await Writer.RecordAsync([SyncAttentionItemFactory.CreateConflict(id, FirstAccount, SyncPaths.Of("Docs/A.txt"), ConflictKindFactory.CreateBothModified(), local, remote, Moment)], CancellationToken);

        var item = (await Open(FirstAccount)).ShouldHaveSingleItem().ShouldBeOfType<SyncAttentionItem.Conflict>();

        (item.Id, item.Path.Value, item.Kind, item.Local, item.Remote, item.DetectedAt).ShouldBe((id, "Docs/A.txt", ConflictKindFactory.CreateBothModified(), local, remote, Moment));
    }

    [Fact]
    public async Task when_a_conflict_has_an_absent_side_then_the_absence_reads_back()
    {
        _ = await Writer.RecordAsync([Conflict(FirstAccount, "Docs/A.txt", ConflictKindFactory.CreateLocalModifiedRemoteDeleted(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent())], CancellationToken);

        var item = (await Open(FirstAccount)).ShouldHaveSingleItem().ShouldBeOfType<SyncAttentionItem.Conflict>();

        (item.Local, item.Remote).ShouldBe((SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent()));
    }

    [Fact]
    public async Task when_the_same_path_is_recorded_again_in_another_case_then_the_row_is_updated_and_keeps_its_id_and_first_detection_time()
    {
        var first = Conflict(FirstAccount, "Docs/A.txt", ConflictKindFactory.CreateBothAdded(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent());
        _ = await Writer.RecordAsync([first], CancellationToken);
        var fresh = SideSnapshotFactory.CreatePresent(7, Moment.AddHours(1), Option.Some(new ContentHash("hash-2")), Option.None<ContentTag>());
        var again = SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), FirstAccount, SyncPaths.Of("DOCS/a.txt"), ConflictKindFactory.CreateBothModified(), fresh, SideSnapshotFactory.CreateAbsent(), Moment.AddHours(1));

        _ = await Writer.RecordAsync([again], CancellationToken);

        var item = (await Open(FirstAccount)).ShouldHaveSingleItem().ShouldBeOfType<SyncAttentionItem.Conflict>();
        (item.Id, item.Kind, item.Local, item.DetectedAt).ShouldBe((((SyncAttentionItem.Conflict)first).Id, ConflictKindFactory.CreateBothModified(), fresh, Moment));
    }

    [Fact]
    public async Task when_many_passes_record_the_same_path_at_once_then_one_row_remains()
    {
        var items = Enumerable.Range(0, 20).Select(index => Conflict(FirstAccount, index % 2 == 0 ? "Docs/A.txt" : "DOCS/a.TXT", ConflictKindFactory.CreateBothAdded(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent())).ToList();

        var results = await Task.WhenAll(items.Select(item => Task.Run(() => Writer.RecordAsync([item], CancellationToken), CancellationToken)));

        (results.All(result => result.Match(_ => true, _ => false)), (await Open(FirstAccount)).Count).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_the_same_path_is_conflicted_on_two_accounts_then_each_account_has_its_own_row()
    {
        _ = await Writer.RecordAsync([Conflict(FirstAccount, "Docs/A.txt"), Conflict(SecondAccount, "Docs/A.txt")], CancellationToken);

        ((await Open(FirstAccount)).Count, (await Open(SecondAccount)).Count).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_deletions_are_held_then_they_read_back_sorted_with_their_pair_and_direction()
    {
        var id = new HeldDeletionId(Guid.NewGuid());
        _ = await Writer.RecordAsync([SyncAttentionItemFactory.CreateHeldDeletions(id, FirstAccount, Pair, DeletionDirectionFactory.CreateDeleteRemote(), [SyncPaths.Of("b.txt"), SyncPaths.Of("A.txt")], Moment)], CancellationToken);

        var item = (await Open(FirstAccount)).ShouldHaveSingleItem().ShouldBeOfType<SyncAttentionItem.HeldDeletions>();

        (item.Id, item.PairId, item.Direction, string.Join(',', item.Paths.Select(path => path.Value))).ShouldBe((id, Pair, DeletionDirectionFactory.CreateDeleteRemote(), "A.txt,b.txt"));
    }

    [Fact]
    public async Task when_the_database_is_closed_and_reopened_then_the_held_deletions_and_conflicts_are_still_there()
    {
        var id = new HeldDeletionId(Guid.NewGuid());
        var held = SyncAttentionItemFactory.CreateHeldDeletions(id, FirstAccount, Pair, DeletionDirectionFactory.CreateDeleteRemote(), [SyncPaths.Of("b.txt"), SyncPaths.Of("A.txt")], Moment);
        _ = await Writer.RecordAsync([held, Conflict(FirstAccount, "Docs/A.txt")], CancellationToken);

        await Database.RestartAsync();

        var items = await Open(FirstAccount);
        var restored = items.OfType<SyncAttentionItem.HeldDeletions>().ShouldHaveSingleItem();
        (items.Count, restored.Id, restored.PairId, restored.Direction, string.Join(',', restored.Paths.Select(path => path.Value))).ShouldBe((2, id, Pair, DeletionDirectionFactory.CreateDeleteRemote(), "A.txt,b.txt"));
    }

    [Fact]
    public async Task when_deletions_are_held_again_for_the_same_pair_and_direction_then_the_paths_are_replaced_and_not_appended()
    {
        _ = await Writer.RecordAsync([Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "a.txt", "b.txt")], CancellationToken);

        _ = await Writer.RecordAsync([Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "c.txt")], CancellationToken);

        var item = (await Open(FirstAccount)).ShouldHaveSingleItem().ShouldBeOfType<SyncAttentionItem.HeldDeletions>();
        string.Join(',', item.Paths.Select(path => path.Value)).ShouldBe("c.txt");
    }

    [Fact]
    public async Task when_deletions_are_held_in_both_directions_then_each_direction_has_its_own_row()
    {
        _ = await Writer.RecordAsync([Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "a.txt"), Held(FirstAccount, DeletionDirectionFactory.CreateDeleteRemote(), "a.txt")], CancellationToken);

        (await Open(FirstAccount)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_many_passes_hold_the_same_pair_at_once_then_one_row_remains()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(index => Task.Run(() => Writer.RecordAsync([Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), $"file{index}.txt")], CancellationToken), CancellationToken)));

        (results.All(result => result.Match(_ => true, _ => false)), (await Open(FirstAccount)).Count).ShouldBe((true, 1));
    }

    [Fact]
    public async Task when_one_item_of_a_batch_cannot_be_stored_then_nothing_of_the_batch_is_stored()
    {
        await CreateTriggerRejectingPathAsync("Boom/Bad.txt");

        var result = await Writer.RecordAsync([Conflict(FirstAccount, "Docs/A.txt"), Conflict(FirstAccount, "Boom/Bad.txt")], CancellationToken);

        (result.Match(_ => true, _ => false), (await Open(FirstAccount)).Count, await RowCountAsync()).ShouldBe((false, 0, 0));
    }

    [Fact]
    public async Task when_a_conflict_has_no_path_then_the_write_fails_and_nothing_of_the_batch_is_stored()
    {
        var noPath = SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), FirstAccount, default, ConflictKindFactory.CreateBothAdded(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent(), Moment);

        var result = await Writer.RecordAsync([Conflict(FirstAccount, "Docs/A.txt"), noPath], CancellationToken);

        (result.Match(_ => true, _ => false), await RowCountAsync()).ShouldBe((false, 0));
    }

    [Fact]
    public async Task when_the_write_is_cancelled_then_nothing_is_stored()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var thrown = await Record.ExceptionAsync(async () => await Writer.RecordAsync([Conflict(FirstAccount, "Docs/A.txt")], source.Token));

        (thrown is null or OperationCanceledException, await RowCountAsync()).ShouldBe((true, 0));
    }

    [Fact]
    public async Task when_items_are_recorded_then_the_rows_are_in_the_database_file()
    {
        _ = await Writer.RecordAsync([Conflict(FirstAccount, "Docs/A.txt"), Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "a.txt", "b.txt")], CancellationToken);

        (await RowCountAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task when_items_are_recorded_then_the_counts_and_the_conflicted_paths_are_per_account()
    {
        _ = await Writer.RecordAsync([Conflict(FirstAccount, "Docs/A.txt"), Conflict(FirstAccount, "Docs/B.txt"), Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "a.txt", "b.txt", "c.txt"), Conflict(SecondAccount, "Other.txt")], CancellationToken);

        var counts = await Reader.CountAsync(FirstAccount, CancellationToken);
        var paths = await Reader.GetConflictedPathsAsync(FirstAccount, CancellationToken);

        (counts.Match(found => (found.Conflicts, found.HeldDeletionSets, found.HeldDeletionPaths), _ => (-1, -1, -1)), string.Join(',', paths.Match(found => found.Select(path => path.Value), _ => [])))
            .ShouldBe(((2, 1, 3), "Docs/A.txt,Docs/B.txt"));
    }

    [Fact]
    public async Task when_an_account_has_no_items_then_the_counts_are_zero_and_the_lists_are_empty()
    {
        var counts = await Reader.CountAsync(FirstAccount, CancellationToken);

        (counts.Match(found => (found.Conflicts, found.HeldDeletionSets, found.HeldDeletionPaths), _ => (-1, -1, -1)), (await Open(FirstAccount)).Count).ShouldBe(((0, 0, 0), 0));
    }

    [Fact]
    public async Task when_items_are_recorded_oldest_first_is_the_read_order()
    {
        _ = await Writer.RecordAsync([ConflictAt(FirstAccount, "Docs/Late.txt", Moment.AddHours(2)), ConflictAt(FirstAccount, "Docs/Early.txt", Moment)], CancellationToken);

        var order = (await Open(FirstAccount)).Select(item => ((SyncAttentionItem.Conflict)item).Path.Value);

        string.Join(',', order).ShouldBe("Docs/Early.txt,Docs/Late.txt");
    }

    [Fact]
    public async Task when_a_conflict_is_closed_then_it_is_gone_and_closing_it_again_succeeds()
    {
        var item = (SyncAttentionItem.Conflict)Conflict(FirstAccount, "Docs/A.txt");
        _ = await Writer.RecordAsync([item], CancellationToken);

        var first = await Closer.CloseAsync(item.Id, AttentionResolutionFactory.CreateKeepBoth(), CancellationToken);
        var second = await Closer.CloseAsync(item.Id, AttentionResolutionFactory.CreateKeepBoth(), CancellationToken);

        (first.Match(_ => true, _ => false), second.Match(_ => true, _ => false), (await Open(FirstAccount)).Count).ShouldBe((true, true, 0));
    }

    [Fact]
    public async Task when_a_held_set_is_closed_then_it_and_its_paths_are_gone()
    {
        var item = (SyncAttentionItem.HeldDeletions)Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "a.txt", "b.txt");
        _ = await Writer.RecordAsync([item], CancellationToken);

        var closed = await Closer.CloseAsync(item.Id, AttentionResolutionFactory.CreateConfirmDeletions(), CancellationToken);

        (closed.Match(_ => true, _ => false), (await Open(FirstAccount)).Count, await PathRowCountAsync()).ShouldBe((true, 0, 0));
    }

    [Fact]
    public async Task when_a_conflict_is_closed_then_other_items_stay()
    {
        var closing = (SyncAttentionItem.Conflict)Conflict(FirstAccount, "Docs/A.txt");
        _ = await Writer.RecordAsync([closing, Conflict(FirstAccount, "Docs/B.txt"), Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "a.txt")], CancellationToken);

        _ = await Closer.CloseAsync(closing.Id, AttentionResolutionFactory.CreateKeepLocal(), CancellationToken);

        (await Open(FirstAccount)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_an_account_is_removed_then_only_its_items_and_held_paths_are_deleted()
    {
        _ = await Writer.RecordAsync([Conflict(FirstAccount, "Docs/A.txt"), Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "a.txt"), Conflict(SecondAccount, "Docs/A.txt"), Held(SecondAccount, DeletionDirectionFactory.CreateDeleteLocal(), "a.txt")], CancellationToken);

        var removed = await Participant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        (removed.Match(_ => true, _ => false), (await Open(FirstAccount)).Count, (await Open(SecondAccount)).Count, await PathRowCountAsync()).ShouldBe((true, 0, 2, 1));
    }

    [Fact]
    public async Task when_items_are_recorded_then_the_logs_carry_the_kind_and_no_path_or_raw_account_id()
    {
        _ = await Writer.RecordAsync([Conflict(FirstAccount, "Secret/Plan.docx", ConflictKindFactory.CreateBothAdded()), Held(FirstAccount, DeletionDirectionFactory.CreateDeleteLocal(), "Secret/Other.docx")], CancellationToken);

        var everything = string.Join('\n', Database.Logs.Logger.Messages.Concat(Database.Logs.Logger.Scopes.SelectMany(scope => scope.Values.Select(value => value?.ToString() ?? string.Empty))));

        (everything.Contains("Secret"), everything.Contains(FirstAccount.Value.ToString()), everything.Contains("BothAdded")).ShouldBe((false, false, true));
    }

    [Fact]
    public async Task when_the_database_fails_then_the_read_fails_instead_of_returning_an_empty_queue()
    {
        await DropTableAsync();

        var read = await Reader.GetOpenAsync(FirstAccount, CancellationToken);

        read.Match(_ => "items", error => error.Message).ShouldNotBe("items");
    }

    private static SyncAttentionItem Conflict(AccountId account, string path) => Conflict(account, path, ConflictKindFactory.CreateBothAdded());

    private static SyncAttentionItem Conflict(AccountId account, string path, ConflictKind kind) => Conflict(account, path, kind, SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent());

    private static SyncAttentionItem Conflict(AccountId account, string path, ConflictKind kind, SideSnapshot local, SideSnapshot remote) =>
        SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), account, SyncPaths.Of(path), kind, local, remote, Moment);

    private static SyncAttentionItem ConflictAt(AccountId account, string path, DateTimeOffset detectedAt) =>
        SyncAttentionItemFactory.CreateConflict(new ConflictId(Guid.NewGuid()), account, SyncPaths.Of(path), ConflictKindFactory.CreateBothAdded(), SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent(), detectedAt);

    private static SyncAttentionItem Held(AccountId account, DeletionDirection direction, params string[] paths) =>
        SyncAttentionItemFactory.CreateHeldDeletions(new HeldDeletionId(Guid.NewGuid()), account, Pair, direction, paths.Select(SyncPaths.Of), Moment);

    private async Task<IReadOnlyList<SyncAttentionItem>> Open(AccountId account) =>
        (await Reader.GetOpenAsync(account, CancellationToken)).Match(items => items, _ => throw new InvalidOperationException("The read must succeed."));

    private async Task CreateTriggerRejectingPathAsync(string path)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE TRIGGER RejectPath BEFORE INSERT ON SyncAttention WHEN NEW.Path = '{path}' BEGIN SELECT RAISE(ABORT, 'rejected'); END;";
        _ = await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private async Task DropTableAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DROP TABLE SyncAttentionPaths; DROP TABLE SyncAttention;";
        _ = await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private Task<long> RowCountAsync() => ScalarAsync("SELECT COUNT(*) FROM SyncAttention");

    private Task<long> PathRowCountAsync() => ScalarAsync("SELECT COUNT(*) FROM SyncAttentionPaths");

    private async Task<long> ScalarAsync(string sql)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return (long)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Database.DatabasePath }.ToString());
        await connection.OpenAsync(CancellationToken);

        return connection;
    }
}
