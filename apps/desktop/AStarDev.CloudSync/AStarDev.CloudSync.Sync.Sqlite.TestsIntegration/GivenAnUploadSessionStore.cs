using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAnUploadSessionStore : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTime WrittenAt = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset ExpiresAt = new(2026, 11, 17, 9, 0, 0, TimeSpan.Zero);

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private IFileInfo File(string path) => Database.FileSystem.FileInfo.New(path);

    private static UploadSession Session(long confirmed = 0, string url = "https://upload.example.test/session/one") =>
        UploadSessionFactory.Create(new Uri(url), ExpiresAt, 200_000_000, WrittenAt, confirmed, "new:folder-1/big.bin");

    private async Task<UploadSession?> GetAsync(AccountId accountId, string path) =>
        (await Database.UploadSessions.GetAsync(accountId, File(path), CancellationToken)).Match(found => found.Match(session => (UploadSession?)session, () => null), _ => null);

    private async Task SaveAsync(AccountId accountId, string path, UploadSession session) =>
        (await Database.UploadSessions.SaveAsync(accountId, File(path), session, CancellationToken)).Match(_ => true, _ => throw new InvalidOperationException("The save must work."));

    [Fact]
    public async Task when_sessions_have_expired_then_only_the_expired_ones_are_removed_for_every_account()
    {
        var expired = UploadSessionFactory.Create(new Uri("https://upload.example.test/session/old"), ExpiresAt.AddDays(-10), 200_000_000, WrittenAt, 5, "new:folder-1/old.bin");
        await SaveAsync(FirstAccount, "/sync/old.bin", expired);
        await SaveAsync(SecondAccount, "/sync/older.bin", expired);
        await SaveAsync(FirstAccount, "/sync/live.bin", Session(7));

        var removed = await Database.UploadSessions.RemoveExpiredAsync(ExpiresAt.AddDays(-5), CancellationToken);

        removed.Match(count => count, _ => -1).ShouldBe(2);
        (await GetAsync(FirstAccount, "/sync/old.bin")).ShouldBeNull();
        (await GetAsync(SecondAccount, "/sync/older.bin")).ShouldBeNull();
        (await GetAsync(FirstAccount, "/sync/live.bin")).ShouldNotBeNull();
    }

    [Fact]
    public async Task when_a_session_expires_exactly_now_then_it_is_removed()
    {
        await SaveAsync(FirstAccount, "/sync/edge.bin", Session(1));

        var removed = await Database.UploadSessions.RemoveExpiredAsync(ExpiresAt, CancellationToken);

        removed.Match(count => count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_nothing_has_expired_then_nothing_is_removed()
    {
        await SaveAsync(FirstAccount, "/sync/live.bin", Session(1));

        var removed = await Database.UploadSessions.RemoveExpiredAsync(ExpiresAt.AddDays(-1), CancellationToken);

        removed.Match(count => count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_nothing_was_saved_then_no_session_is_found()
    {
        var found = await GetAsync(FirstAccount, "/sync/big.bin");

        found.ShouldBeNull();
    }

    [Fact]
    public async Task when_a_session_is_saved_then_it_is_read_back_with_every_field()
    {
        await SaveAsync(FirstAccount, "/sync/big.bin", Session(1234));

        var found = await GetAsync(FirstAccount, "/sync/big.bin");

        found.ShouldNotBeNull();
        (found.Url.ToString(), found.ExpiresAt, found.SourceLength, found.SourceWriteUtc, found.ConfirmedBytes, found.Target).ShouldBe(("https://upload.example.test/session/one", ExpiresAt, 200_000_000L, WrittenAt, 1234L, "new:folder-1/big.bin"));
    }

    [Fact]
    public async Task when_a_session_is_saved_twice_then_the_second_replaces_the_first()
    {
        await SaveAsync(FirstAccount, "/sync/big.bin", Session(1));
        await SaveAsync(FirstAccount, "/sync/big.bin", Session(2, "https://upload.example.test/session/two"));

        var found = await GetAsync(FirstAccount, "/sync/big.bin");

        (found?.ConfirmedBytes, found?.Url.ToString()).ShouldBe((2L, "https://upload.example.test/session/two"));
    }

    [Fact]
    public async Task when_the_path_differs_only_in_case_then_the_same_session_is_found()
    {
        await SaveAsync(FirstAccount, "/sync/Big.BIN", Session(7));

        var found = await GetAsync(FirstAccount, "/SYNC/big.bin");

        found?.ConfirmedBytes.ShouldBe(7);
    }

    [Fact]
    public async Task when_two_accounts_save_the_same_path_then_each_keeps_its_own_session()
    {
        await SaveAsync(FirstAccount, "/sync/big.bin", Session(1));
        await SaveAsync(SecondAccount, "/sync/big.bin", Session(2));

        (await GetAsync(FirstAccount, "/sync/big.bin"))?.ConfirmedBytes.ShouldBe(1);
        (await GetAsync(SecondAccount, "/sync/big.bin"))?.ConfirmedBytes.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_session_is_removed_then_it_is_gone_and_other_paths_stay()
    {
        await SaveAsync(FirstAccount, "/sync/big.bin", Session(1));
        await SaveAsync(FirstAccount, "/sync/other.bin", Session(2));

        _ = await Database.UploadSessions.RemoveAsync(FirstAccount, File("/sync/big.bin"), CancellationToken);

        (await GetAsync(FirstAccount, "/sync/big.bin")).ShouldBeNull();
        (await GetAsync(FirstAccount, "/sync/other.bin")).ShouldNotBeNull();
    }

    [Fact]
    public async Task when_a_session_that_is_not_saved_is_removed_then_nothing_fails()
    {
        var result = await Database.UploadSessions.RemoveAsync(FirstAccount, File("/sync/none.bin"), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_app_restarts_then_a_saved_session_survives()
    {
        await SaveAsync(FirstAccount, "/sync/big.bin", Session(99));

        await Database.RestartAsync();

        (await GetAsync(FirstAccount, "/sync/big.bin"))?.ConfirmedBytes.ShouldBe(99);
    }

    [Fact]
    public async Task when_an_account_is_removed_then_only_its_sessions_are_deleted()
    {
        await SaveAsync(FirstAccount, "/sync/big.bin", Session(1));
        await SaveAsync(SecondAccount, "/sync/big.bin", Session(2));

        _ = await Database.UploadSessionsParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        (await GetAsync(FirstAccount, "/sync/big.bin")).ShouldBeNull();
        (await GetAsync(SecondAccount, "/sync/big.bin")).ShouldNotBeNull();
    }

    [Fact]
    public async Task when_twenty_saves_removes_and_reads_of_one_path_race_then_every_call_succeeds_and_one_consistent_row_is_left()
    {
        var calls = Enumerable.Range(0, 20).Select(index => Task.Run(async () =>
        {
            var save = await Database.UploadSessions.SaveAsync(FirstAccount, File("/sync/Big.bin"), Session(index), CancellationToken);
            var read = await Database.UploadSessions.GetAsync(FirstAccount, File("/SYNC/big.bin"), CancellationToken);
            var remove = index % 5 == 0 ? await Database.UploadSessions.RemoveAsync(FirstAccount, File("/sync/big.bin"), CancellationToken) : Result.Success<Unit, ErrorResponse>(Unit.Instance);

            return save.Match(_ => true, _ => false) && read.Match(_ => true, _ => false) && remove.Match(_ => true, _ => false);
        }, CancellationToken));

        var outcomes = await Task.WhenAll(calls);
        await SaveAsync(FirstAccount, "/sync/big.bin", Session(500));

        outcomes.ShouldAllBe(succeeded => succeeded);
        (await GetAsync(FirstAccount, "/sync/BIG.bin"))?.ConfirmedBytes.ShouldBe(500);
    }
}
