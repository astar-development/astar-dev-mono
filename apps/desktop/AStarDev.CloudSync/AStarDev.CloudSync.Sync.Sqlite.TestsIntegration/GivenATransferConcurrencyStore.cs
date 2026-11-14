using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenATransferConcurrencyStore : IAsyncLifetime
{
    private static readonly AccountId FirstAccount = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId SecondAccount = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private TemporarySyncDatabase? database;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private TemporarySyncDatabase Database => database ?? throw new InvalidOperationException("The database is created before each test.");

    public async ValueTask InitializeAsync() => database = await TemporarySyncDatabase.CreateMigratedAsync(CancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (database is not null)
            await database.DisposeAsync();
    }

    private async Task<int> ReadAsync(AccountId accountId) => (await Database.ConcurrencyReader.GetAsync(accountId, CancellationToken)).Match(found => found.Value, _ => -1);

    private async Task SetAsync(AccountId accountId, int requested) => _ = await Database.ConcurrencyWriter.SetAsync(accountId, TransferConcurrencyFactory.Create(requested), CancellationToken);

    [Fact]
    public async Task when_an_account_never_changed_it_then_it_has_the_default_of_four()
    {
        (await ReadAsync(FirstAccount)).ShouldBe(4);
    }

    [Fact]
    public async Task when_an_account_sets_a_value_then_it_is_read_back()
    {
        await SetAsync(FirstAccount, 6);

        (await ReadAsync(FirstAccount)).ShouldBe(6);
    }

    [Fact]
    public async Task when_an_account_sets_a_value_twice_then_the_last_one_wins()
    {
        await SetAsync(FirstAccount, 6);
        await SetAsync(FirstAccount, 2);

        (await ReadAsync(FirstAccount)).ShouldBe(2);
    }

    [Fact]
    public async Task when_one_account_changes_it_then_other_accounts_keep_the_default()
    {
        await SetAsync(FirstAccount, 8);

        (await ReadAsync(FirstAccount), await ReadAsync(SecondAccount)).ShouldBe((8, 4));
    }

    [Fact]
    public async Task when_a_value_outside_the_range_is_set_then_the_clamped_value_is_stored()
    {
        await SetAsync(FirstAccount, 50);
        await SetAsync(SecondAccount, 0);

        (await ReadAsync(FirstAccount), await ReadAsync(SecondAccount)).ShouldBe((8, 1));
    }

    [Fact]
    public async Task when_the_account_is_removed_then_its_setting_goes_back_to_the_default()
    {
        await SetAsync(FirstAccount, 7);

        var removed = await Database.ConcurrencyParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        (removed.Match(_ => true, _ => false), await ReadAsync(FirstAccount)).ShouldBe((true, 4));
    }
}
