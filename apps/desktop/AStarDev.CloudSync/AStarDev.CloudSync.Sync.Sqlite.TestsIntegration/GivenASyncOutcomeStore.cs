using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASyncOutcomeStore : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private TemporarySyncDatabase? database;

    public static TheoryData<SyncStatus> RestingStatuses => new()
    {
        SyncStatusFactory.CreateUpToDate(),
        SyncStatusFactory.CreateNeedsAttention(3, 0),
        SyncStatusFactory.CreateNeedsAttention(0, 61),
        SyncStatusFactory.CreateNeedsAttention(2, 5),
        SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateCouldNotStart()),
        SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateAuthenticationRequired()),
        SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateRootUnavailable()),
        SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded()),
        SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateInsufficientDiskSpace()),
        SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateCancelled()),
        SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateUnexpected()),
    };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    private ISyncOutcomeReader Reader => Database.Services.GetRequiredService<ISyncOutcomeReader>();

    private ISyncOutcomeWriter Writer => Database.Services.GetRequiredService<ISyncOutcomeWriter>();

    private IAccountRemovalParticipant Participant => Database.Services.GetServices<IAccountRemovalParticipant>().First(participant => participant.GetType().Name == "SqliteSyncOutcomeStore");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private async Task<IReadOnlyDictionary<AccountId, SyncStatus>> ReadAsync() =>
        (await Reader.GetAllAsync(CancellationToken)).Match(statuses => statuses, error => throw new InvalidOperationException(error.Message));

    [Fact]
    public async Task when_nothing_was_saved_then_no_account_has_a_status()
    {
        var statuses = await ReadAsync();

        statuses.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(RestingStatuses))]
    public async Task when_a_status_is_saved_then_the_same_status_is_read_back(SyncStatus status)
    {
        var saved = await Writer.SaveAsync(FirstAccount, status, CancellationToken);

        saved.Match(_ => true, _ => false).ShouldBeTrue();
        (await ReadAsync()).ShouldBe(new Dictionary<AccountId, SyncStatus> { [FirstAccount] = status });
    }

    [Fact]
    public async Task when_a_status_is_saved_twice_then_the_later_one_replaces_the_earlier_one()
    {
        _ = await Writer.SaveAsync(FirstAccount, SyncStatusFactory.CreateNeedsAttention(4, 9), CancellationToken);

        _ = await Writer.SaveAsync(FirstAccount, SyncStatusFactory.CreateUpToDate(), CancellationToken);

        (await ReadAsync()).ShouldBe(new Dictionary<AccountId, SyncStatus> { [FirstAccount] = SyncStatusFactory.CreateUpToDate() });
    }

    [Fact]
    public async Task when_a_status_that_is_not_a_resting_status_is_saved_then_up_to_date_is_stored()
    {
        _ = await Writer.SaveAsync(FirstAccount, SyncStatusFactory.CreateSyncing(40), CancellationToken);

        (await ReadAsync())[FirstAccount].ShouldBe(SyncStatusFactory.CreateUpToDate());
    }

    [Fact]
    public async Task when_a_status_is_saved_then_changed_is_raised_once()
    {
        var raised = 0;
        Reader.Changed += (_, _) => raised++;

        _ = await Writer.SaveAsync(FirstAccount, SyncStatusFactory.CreateUpToDate(), CancellationToken);

        raised.ShouldBe(1);
    }

    [Fact]
    public async Task when_an_accounts_data_is_removed_then_only_its_status_is_deleted()
    {
        _ = await Writer.SaveAsync(FirstAccount, SyncStatusFactory.CreateFailed(SyncFailureReasonFactory.CreateQuotaExceeded()), CancellationToken);
        _ = await Writer.SaveAsync(SecondAccount, SyncStatusFactory.CreateNeedsAttention(1, 0), CancellationToken);

        var removed = await Participant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
        (await ReadAsync()).ShouldBe(new Dictionary<AccountId, SyncStatus> { [SecondAccount] = SyncStatusFactory.CreateNeedsAttention(1, 0) });
    }

    [Fact]
    public async Task when_twenty_saves_race_for_one_account_then_every_save_succeeds_and_one_row_is_left()
    {
        var saves = Enumerable.Range(0, 20).Select(index => Task.Run(() => Writer.SaveAsync(FirstAccount, SyncStatusFactory.CreateNeedsAttention(index + 1, 0), CancellationToken), CancellationToken));

        var results = await Task.WhenAll(saves);

        results.Count(result => result.Match(_ => true, _ => false)).ShouldBe(20);
        (await ReadAsync()).Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_database_was_never_migrated_then_reading_and_saving_fail_without_throwing()
    {
        await using var unmigrated = TemporarySyncDatabase.CreateUnmigrated();

        var read = await unmigrated.Services.GetRequiredService<ISyncOutcomeReader>().GetAllAsync(CancellationToken);
        var saved = await unmigrated.Services.GetRequiredService<ISyncOutcomeWriter>().SaveAsync(FirstAccount, SyncStatusFactory.CreateUpToDate(), CancellationToken);

        (read.Match(_ => "read", _ => "failed"), saved.Match(_ => "saved", _ => "failed")).ShouldBe(("failed", "failed"));
    }
}
