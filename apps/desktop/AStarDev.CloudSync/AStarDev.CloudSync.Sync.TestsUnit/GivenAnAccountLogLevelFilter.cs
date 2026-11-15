using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnAccountLogLevelFilter
{
    private static readonly AccountId Photos = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId Work = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private readonly AccountLogLevels levels = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static Dictionary<string, object?> ScopeOf(AccountId accountId) => new() { [SyncLogScope.AccountIdHashKey] = LogHash.Of(accountId.Value.ToString()) };

    [Theory]
    [InlineData(LogLevel.Trace, false)]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Information, false)]
    [InlineData(LogLevel.Warning, true)]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Critical, true)]
    public void when_a_standard_account_logs_then_only_warnings_and_errors_are_written(LogLevel level, bool expected)
    {
        AccountLogLevelFilter.Admits(levels, level, ScopeOf(Photos)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public void when_a_verbose_account_logs_then_every_level_is_written(LogLevel level)
    {
        levels.Apply(Photos, AccountLogLevelFactory.CreateVerbose());

        AccountLogLevelFilter.Admits(levels, level, ScopeOf(Photos)).ShouldBeTrue();
    }

    [Fact]
    public void when_one_account_is_verbose_then_another_account_stays_standard()
    {
        levels.Apply(Photos, AccountLogLevelFactory.CreateVerbose());

        AccountLogLevelFilter.Admits(levels, LogLevel.Debug, ScopeOf(Work)).ShouldBeFalse();
    }

    [Fact]
    public void when_the_level_is_toggled_then_the_next_entry_follows_it_without_a_restart()
    {
        var scope = ScopeOf(Photos);

        levels.Apply(Photos, AccountLogLevelFactory.CreateVerbose());
        var whileVerbose = AccountLogLevelFilter.Admits(levels, LogLevel.Debug, scope);
        levels.Apply(Photos, AccountLogLevelFactory.CreateStandard());
        var afterwards = AccountLogLevelFilter.Admits(levels, LogLevel.Debug, scope);

        (whileVerbose, afterwards).ShouldBe((true, false));
    }

    [Theory]
    [InlineData(LogLevel.Trace, false)]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Information, true)]
    [InlineData(LogLevel.Warning, true)]
    public void when_an_entry_belongs_to_no_account_then_information_and_above_is_written(LogLevel level, bool expected)
    {
        AccountLogLevelFilter.Admits(levels, level, new Dictionary<string, object?>()).ShouldBe(expected);
    }

    [Fact]
    public void when_the_account_scope_is_not_text_then_the_entry_counts_as_belonging_to_no_account()
    {
        var scope = new Dictionary<string, object?> { [SyncLogScope.AccountIdHashKey] = 42 };

        (AccountLogLevelFilter.Admits(levels, LogLevel.Information, scope), AccountLogLevelFilter.Admits(levels, LogLevel.Debug, scope)).ShouldBe((true, false));
    }

    [Fact]
    public void when_a_hash_is_unknown_then_its_level_is_standard()
    {
        levels.LevelOf("0000000000000000").ShouldBe(AccountLogLevelFactory.CreateStandard());
    }

    [Fact]
    public async Task when_the_levels_are_loaded_then_the_verbose_accounts_are_verbose()
    {
        var settings = new ListingSettings([Photos]);

        var loaded = await levels.LoadAsync(settings, CancellationToken);

        (loaded.Match(_ => true, _ => false), levels.LevelOf(LogHash.Of(Photos.Value.ToString())), levels.LevelOf(LogHash.Of(Work.Value.ToString()))).ShouldBe((true, AccountLogLevelFactory.CreateVerbose(), AccountLogLevelFactory.CreateStandard()));
    }

    [Fact]
    public async Task when_the_settings_cannot_be_read_then_the_load_fails_and_every_account_stays_standard()
    {
        var loaded = await levels.LoadAsync(new ListingSettings(null), CancellationToken);

        (loaded.Match(_ => true, _ => false), levels.LevelOf(LogHash.Of(Photos.Value.ToString()))).ShouldBe((false, AccountLogLevelFactory.CreateStandard()));
    }

    [Fact]
    public void when_many_threads_apply_levels_at_once_then_every_account_ends_with_its_own_level()
    {
        var accounts = Enumerable.Range(0, 200).Select(index => AccountIdFactory.Create(Guid.NewGuid())).ToList();

        Parallel.ForEach(accounts, new ParallelOptions { CancellationToken = CancellationToken }, account => levels.Apply(account, AccountLogLevelFactory.CreateVerbose()));

        accounts.Count(account => levels.LevelOf(LogHash.Of(account.Value.ToString())) is AccountLogLevel.Verbose).ShouldBe(200);
    }

    private sealed class ListingSettings(IReadOnlyList<AccountId>? verbose) : IAccountLogLevelSettings
    {
        public Task<Result<AccountLogLevel, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<Unit, ErrorResponse>> SetAsync(AccountId accountId, AccountLogLevel level, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> ListVerboseAsync(CancellationToken cancellationToken) => Task.FromResult(verbose is null
            ? Result.Failure<IReadOnlyList<AccountId>, ErrorResponse>(new ErrorResponse("The settings could not be read."))
            : Result.Success<IReadOnlyList<AccountId>, ErrorResponse>(verbose));
    }
}
