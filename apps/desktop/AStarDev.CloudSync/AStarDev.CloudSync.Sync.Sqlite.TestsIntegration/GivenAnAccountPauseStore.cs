using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenAnAccountPauseStore : IAsyncLifetime
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

    private async Task<IReadOnlyList<AccountId>> PausedAsync() => (await Database.PauseReader.GetPausedAsync(CancellationToken)).Match(found => found, _ => []);

    private async Task<object> ToggleAsync(AccountId accountId) => (await Database.PauseWriter.ToggleAsync(accountId, CancellationToken)).Match(state => (object)state, error => error);

    [Fact]
    public async Task when_no_account_was_paused_then_the_paused_list_is_empty()
    {
        (await PausedAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_active_account_is_toggled_then_it_is_paused_and_listed()
    {
        var state = await ToggleAsync(FirstAccount);

        state.ShouldBe(PauseStateFactory.CreatePaused());
        (await PausedAsync()).ShouldBe([FirstAccount]);
    }

    [Fact]
    public async Task when_a_paused_account_is_toggled_then_it_is_active_and_no_longer_listed()
    {
        _ = await ToggleAsync(FirstAccount);

        var state = await ToggleAsync(FirstAccount);

        state.ShouldBe(PauseStateFactory.CreateActive());
        (await PausedAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_one_account_is_paused_then_the_other_is_not()
    {
        _ = await ToggleAsync(FirstAccount);

        var state = await ToggleAsync(SecondAccount);

        state.ShouldBe(PauseStateFactory.CreatePaused());
        (await PausedAsync()).ShouldBe([FirstAccount, SecondAccount], ignoreOrder: true);
    }

    [Fact]
    public async Task when_the_account_is_removed_then_its_pause_is_forgotten_and_other_accounts_keep_theirs()
    {
        _ = await ToggleAsync(FirstAccount);
        _ = await ToggleAsync(SecondAccount);

        var removed = await Database.PauseParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
        (await PausedAsync()).ShouldBe([SecondAccount]);
    }

    [Fact]
    public async Task when_an_account_with_no_pause_is_removed_then_the_removal_succeeds()
    {
        var removed = await Database.PauseParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_twenty_toggles_race_for_one_account_then_an_even_number_leaves_it_active_and_none_failed()
    {
        var racing = Enumerable.Range(0, 20).Select(_ => Task.Run(() => ToggleAsync(FirstAccount), CancellationToken));

        var outcomes = await Task.WhenAll(racing);

        outcomes.ShouldAllBe(outcome => outcome is PauseState);
        (await PausedAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_database_is_unreadable_then_the_read_and_the_toggle_fail_without_throwing_and_without_the_path_in_the_log()
    {
        await using var broken = TemporarySyncDatabase.CreateUnmigrated();

        var paused = await broken.PauseReader.GetPausedAsync(CancellationToken);
        var toggled = await broken.PauseWriter.ToggleAsync(FirstAccount, CancellationToken);

        paused.Match(_ => false, _ => true).ShouldBeTrue();
        toggled.Match(_ => false, _ => true).ShouldBeTrue();
        broken.Logs.Logger.Messages.ShouldNotContain(message => message.Contains(broken.DatabasePath));
    }
}
