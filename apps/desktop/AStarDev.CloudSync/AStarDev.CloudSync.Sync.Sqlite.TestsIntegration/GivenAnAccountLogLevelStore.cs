using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAnAccountLogLevelStore : IAsyncLifetime
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

    private async Task<AccountLogLevel> ReadAsync(AccountId accountId) => (await Database.LogLevels.GetAsync(accountId, CancellationToken)).Match(found => found, _ => throw new InvalidOperationException("The read must succeed."));

    private async Task SetAsync(AccountId accountId, AccountLogLevel level) => _ = await Database.LogLevels.SetAsync(accountId, level, CancellationToken);

    [Fact]
    public async Task when_an_account_never_changed_it_then_it_is_standard()
    {
        (await ReadAsync(FirstAccount)).ShouldBe(AccountLogLevelFactory.CreateStandard());
    }

    [Fact]
    public async Task when_an_account_is_made_verbose_then_it_is_read_back_as_verbose()
    {
        await SetAsync(FirstAccount, AccountLogLevelFactory.CreateVerbose());

        (await ReadAsync(FirstAccount)).ShouldBe(AccountLogLevelFactory.CreateVerbose());
    }

    [Fact]
    public async Task when_an_account_is_made_verbose_and_then_standard_then_the_last_one_wins()
    {
        await SetAsync(FirstAccount, AccountLogLevelFactory.CreateVerbose());
        await SetAsync(FirstAccount, AccountLogLevelFactory.CreateStandard());

        (await ReadAsync(FirstAccount)).ShouldBe(AccountLogLevelFactory.CreateStandard());
    }

    [Fact]
    public async Task when_one_account_is_made_verbose_then_another_stays_standard()
    {
        await SetAsync(FirstAccount, AccountLogLevelFactory.CreateVerbose());

        (await ReadAsync(SecondAccount)).ShouldBe(AccountLogLevelFactory.CreateStandard());
    }

    [Fact]
    public async Task when_the_level_is_set_then_the_running_logging_follows_it_at_once()
    {
        await SetAsync(FirstAccount, AccountLogLevelFactory.CreateVerbose());
        var verbose = Database.RunningLevels.LevelOf(LogHash.Of(FirstAccount.Value.ToString()));
        await SetAsync(FirstAccount, AccountLogLevelFactory.CreateStandard());
        var standard = Database.RunningLevels.LevelOf(LogHash.Of(FirstAccount.Value.ToString()));

        (verbose, standard).ShouldBe((AccountLogLevelFactory.CreateVerbose(), AccountLogLevelFactory.CreateStandard()));
    }

    [Fact]
    public async Task when_the_verbose_accounts_are_listed_then_only_those_are_returned()
    {
        await SetAsync(FirstAccount, AccountLogLevelFactory.CreateVerbose());
        await SetAsync(SecondAccount, AccountLogLevelFactory.CreateStandard());

        var listed = await Database.LogLevels.ListVerboseAsync(CancellationToken);

        listed.Match(accounts => accounts.Select(account => account.Value).ToList(), _ => []).ShouldBe([FirstAccount.Value]);
    }

    [Fact]
    public async Task when_the_account_is_removed_then_it_is_standard_again_and_the_logging_follows()
    {
        await SetAsync(FirstAccount, AccountLogLevelFactory.CreateVerbose());

        var removed = await Database.LogLevelParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        (removed.Match(_ => true, _ => false), await ReadAsync(FirstAccount), Database.RunningLevels.LevelOf(LogHash.Of(FirstAccount.Value.ToString()))).ShouldBe((true, AccountLogLevelFactory.CreateStandard(), AccountLogLevelFactory.CreateStandard()));
    }

    [Fact]
    public async Task when_many_accounts_are_set_at_once_then_each_keeps_its_own_level()
    {
        var accounts = Enumerable.Range(0, 20).Select(_ => AccountIdFactory.Create(Guid.NewGuid())).ToList();

        await Task.WhenAll(accounts.Select((account, index) => Task.Run(() => SetAsync(account, index % 2 == 0 ? AccountLogLevelFactory.CreateVerbose() : AccountLogLevelFactory.CreateStandard()), CancellationToken)));

        var verbose = (await Database.LogLevels.ListVerboseAsync(CancellationToken)).Match(listed => listed.Count, _ => -1);
        verbose.ShouldBe(10);
    }
}
