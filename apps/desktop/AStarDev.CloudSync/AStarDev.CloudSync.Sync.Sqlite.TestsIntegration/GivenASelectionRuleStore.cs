using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class GivenASelectionRuleStore : IAsyncLifetime
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

    [Fact]
    public async Task when_an_account_has_no_rules_then_the_list_is_empty()
    {
        var rules = await Database.Rules.GetAsync(FirstAccount, CancellationToken);

        rules.Match(found => found.Count, _ => -1).ShouldBe(0);
    }

    [Fact]
    public async Task when_a_folder_is_included_then_the_rule_is_read_back_with_its_remote_id_and_direction()
    {
        var remoteId = RemoteItemIdFactory.Create("remote-1").Match(Option.Some, _ => Option.None<RemoteItemId>());

        var applied = await Database.RuleWriter.ApplyAsync(FirstAccount, [SelectionChangeFactory.CreateIncludeFolder(Path("Docs/Work"), remoteId, SyncDirectionFactory.CreateUploadOnly())], CancellationToken);

        applied.Match(_ => true, _ => false).ShouldBeTrue();
        (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found, _ => []).ShouldBe([SelectionRuleFactory.Create(FirstAccount, Path("Docs/Work"), remoteId, SelectionRuleKindFactory.CreateInclude(SyncDirectionFactory.CreateUploadOnly()))]);
    }

    [Fact]
    public async Task when_rules_are_saved_then_they_are_read_back_sorted_ignoring_case_with_their_kinds()
    {
        _ = await Apply(FirstAccount, Include("zeta"), Exclude("Alpha"), Include("beta", SyncDirectionFactory.CreateDownloadOnly()));

        var rules = (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found, _ => []);

        rules.Select(rule => (rule.Path.Value, rule.Kind)).ShouldBe([("Alpha", SelectionRuleKindFactory.CreateExclude()), ("beta", SelectionRuleKindFactory.CreateInclude(SyncDirectionFactory.CreateDownloadOnly())), ("zeta", SelectionRuleKindFactory.CreateInclude(SyncDirectionFactory.CreateTwoWay()))]);
    }

    [Fact]
    public async Task when_a_rule_is_set_again_in_another_casing_then_one_rule_remains_with_the_latest_kind()
    {
        _ = await Apply(FirstAccount, Include("Docs"));
        _ = await Apply(FirstAccount, Exclude("docs"));

        var rules = (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found, _ => []);

        rules.Select(rule => rule.Kind).ShouldBe([SelectionRuleKindFactory.CreateExclude()]);
    }

    [Fact]
    public async Task when_a_folder_is_included_then_the_rules_below_it_are_removed()
    {
        _ = await Apply(FirstAccount, Include("Docs"), Exclude("Docs/Private"), Include("Docs/Private/Keep"));
        _ = await Apply(FirstAccount, Include("Docs"));

        (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found.Select(rule => rule.Path.Value), _ => []).ShouldBe(["Docs"]);
    }

    [Fact]
    public async Task when_a_pair_changes_direction_then_the_new_direction_is_saved()
    {
        _ = await Apply(FirstAccount, Include("Docs"));

        _ = await Database.RuleWriter.ApplyAsync(FirstAccount, [SelectionChangeFactory.CreateChangeDirection(SyncPairIdFactory.CreateForRule(FirstAccount, Path("Docs")), SyncDirectionFactory.CreateDownloadOnly())], CancellationToken);

        (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found.Select(rule => rule.Kind), _ => []).ShouldBe([SelectionRuleKindFactory.CreateInclude(SyncDirectionFactory.CreateDownloadOnly())]);
    }

    [Fact]
    public async Task when_two_accounts_have_rules_then_each_sees_only_its_own()
    {
        _ = await Apply(FirstAccount, Include("Docs"));
        _ = await Apply(SecondAccount, Exclude("Photos"));

        var first = (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found.Select(rule => rule.Path.Value), _ => []);
        var second = (await Database.Rules.GetAsync(SecondAccount, CancellationToken)).Match(found => found.Select(rule => rule.Path.Value), _ => []);

        (first, second).ShouldBe((["Docs"], ["Photos"]));
    }

    [Fact]
    public async Task when_twenty_saves_for_one_account_run_at_once_then_every_one_succeeds_and_none_is_lost()
    {
        var folders = Enumerable.Range(0, 20).Select(number => $"folder-{number:00}").ToList();
        var start = new TaskCompletionSource();
        var pending = folders.Select(folder => Task.Run(async () => await start.Task.ContinueWith(_ => Apply(FirstAccount, Include(folder)), CancellationToken).Unwrap(), CancellationToken)).ToList();

        start.SetResult();
        var results = await Task.WhenAll(pending);

        results.ShouldAllBe(result => result.Match(_ => true, _ => false));
        (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found.Select(rule => rule.Path.Value).ToList(), _ => []).ShouldBe(folders);
    }

    [Fact]
    public async Task when_saves_that_toggle_one_folder_in_different_casings_run_at_once_then_one_rule_remains_and_none_fails()
    {
        var casings = new[] { "Docs", "docs", "DOCS", "dOcS" };
        var start = new TaskCompletionSource();
        var pending = Enumerable.Range(0, 20).Select(number => Task.Run(async () => await start.Task.ContinueWith(_ => Apply(FirstAccount, number % 2 == 0 ? Include(casings[number % 4]) : Exclude(casings[number % 4])), CancellationToken).Unwrap(), CancellationToken)).ToList();

        start.SetResult();
        var results = await Task.WhenAll(pending);

        results.ShouldAllBe(result => result.Match(_ => true, _ => false));
        (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_an_accounts_data_is_removed_then_its_rules_are_gone_and_the_others_stay()
    {
        _ = await Apply(FirstAccount, Include("Docs"));
        _ = await Apply(SecondAccount, Include("Photos"));

        var removed = await Database.RulesParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
        (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found.Count, _ => -1).ShouldBe(0);
        (await Database.Rules.GetAsync(SecondAccount, CancellationToken)).Match(found => found.Count, _ => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_data_is_removed_for_an_account_without_rules_then_it_succeeds()
    {
        var removed = await Database.RulesParticipant.RemoveAccountDataAsync(FirstAccount, CancellationToken);

        removed.Match(_ => true, _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_reading_and_saving_are_cancelled_and_nothing_is_saved()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var save = () => Database.RuleWriter.ApplyAsync(FirstAccount, [SelectionChangeFactory.CreateExcludeFolder(Path("Docs"), Option.None<RemoteItemId>())], cancelled.Token);
        var read = () => Database.Rules.GetAsync(FirstAccount, cancelled.Token);

        await Should.ThrowAsync<OperationCanceledException>(save);
        await Should.ThrowAsync<OperationCanceledException>(read);
        (await Database.Rules.GetAsync(FirstAccount, CancellationToken)).Match(found => found.Count, _ => -1).ShouldBe(0);
    }

    private static LocalRelativePath Path(string value) => LocalRelativePathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    private static SelectionChange Include(string path, SyncDirection? direction = null) => SelectionChangeFactory.CreateIncludeFolder(Path(path), Option.None<RemoteItemId>(), direction ?? SyncDirectionFactory.CreateTwoWay());

    private static SelectionChange Exclude(string path) => SelectionChangeFactory.CreateExcludeFolder(Path(path), Option.None<RemoteItemId>());

    private Task<Result<Unit, ErrorResponse>> Apply(AccountId accountId, params SelectionChange[] changes) => Database.RuleWriter.ApplyAsync(accountId, changes, CancellationToken);
}
