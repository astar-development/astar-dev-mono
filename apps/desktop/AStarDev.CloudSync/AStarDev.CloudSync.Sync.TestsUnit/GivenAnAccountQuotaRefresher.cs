using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAnAccountQuotaRefresher : IDisposable
{
    private const string Email = "jane.doe@example.com";

    private readonly InMemoryAccountStore store = new();
    private readonly ScriptedQuotaRefresher provider = new();
    private readonly RecordingLoggerProvider logs = new();
    private ServiceProvider? services;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_provider_reports_usage_then_it_is_saved_and_returned()
    {
        var account = await AddAccountAsync("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d");
        provider.Outcome = Result.Success<StorageUsage, ProviderError>(StorageUsageFactory.Create(777, 8_888));

        var result = await Refresher(store).RefreshAsync(account, CancellationToken);

        result.Match(usage => (object)usage, error => error).ShouldBe(StorageUsageFactory.Create(777, 8_888));
        (await StoredStorageAsync(account)).ShouldBe(StorageUsageFactory.Create(777, 8_888));
    }

    [Fact]
    public async Task when_the_provider_cannot_say_then_the_last_saved_usage_stays_a_warning_is_logged_and_the_error_is_returned()
    {
        var account = await AddAccountAsync("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d");
        provider.Outcome = Result.Failure<StorageUsage, ProviderError>(ProviderErrorFactory.CreateTransient());

        var result = await Refresher(store).RefreshAsync(account, CancellationToken);

        result.Match(_ => (object)"usage", error => error).ShouldBe(ProviderErrorFactory.CreateTransient());
        (await StoredStorageAsync(account)).ShouldBe(StorageUsageFactory.Create(1_024, 5_368_709_120));
        logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logs.Logger.Messages.ShouldContain(message => message.Contains(nameof(ProviderError.Transient), StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_usage_cannot_be_saved_then_a_warning_is_logged_without_account_details_and_the_error_is_transient()
    {
        var account = StoredAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), AccountFixtures.Registration("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d"));
        var failing = new FakeAccountStore
        {
            All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([account]),
            Update = (_, _) => Result.Failure<Unit, ErrorResponse>(new ErrorResponse("db failed for " + Email)),
        };

        var result = await Refresher(failing).RefreshAsync(account.Id, CancellationToken);

        result.Match(_ => (object)"usage", error => error).ShouldBe(ProviderErrorFactory.CreateTransient());
        logs.Logger.Messages.ShouldContain(message => message.Contains("could not be saved", StringComparison.Ordinal));
        logs.Logger.Entries.ShouldAllBe(entry => entry.Level == LogLevel.Warning);
        logs.Logger.Messages.ShouldAllBe(message => !message.Contains(Email, StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_the_account_provider_has_no_quota_reader_then_the_result_is_unsupported_and_a_warning_is_logged()
    {
        var account = await AddAccountAsync("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d");

        var result = await Refresher(store, register: false).RefreshAsync(account, CancellationToken);

        result.Match(_ => (object)"usage", error => error).ShouldBe(ProviderErrorFactory.CreateUnsupported());
        logs.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task when_the_account_is_unknown_then_the_result_is_not_found_and_the_provider_is_not_asked()
    {
        var result = await Refresher(store).RefreshAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        result.Match(_ => (object)"usage", error => error).ShouldBe(ProviderErrorFactory.CreateNotFound());
        provider.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_token_is_already_cancelled_then_the_refresh_is_cancelled_and_nothing_is_saved()
    {
        var account = await AddAccountAsync("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d");
        var refresher = Refresher(store);

        var refresh = () => refresher.RefreshAsync(account, new CancellationToken(true));

        await Should.ThrowAsync<OperationCanceledException>(refresh);
        (await StoredStorageAsync(account)).ShouldBe(StorageUsageFactory.Create(1_024, 5_368_709_120));
    }

    [Fact]
    public async Task when_the_same_account_is_refreshed_many_times_at_once_then_every_refresh_succeeds_and_one_usage_is_stored()
    {
        var account = await AddAccountAsync("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d");
        var refresher = Refresher(store);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => refresher.RefreshAsync(account, CancellationToken), CancellationToken)));

        results.ShouldAllBe(result => result.Match(_ => true, _ => false));
        (await StoredStorageAsync(account)).ShouldBe(StorageUsageFactory.Create(10, 100));
    }

    [Fact]
    public async Task when_the_account_key_casing_differs_between_sign_ins_then_the_same_account_is_refreshed()
    {
        var account = await AddAccountAsync("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d");
        _ = await AddAccountAsync("00000000-0000-0000-A1B2-C3D4E5F60718.9188040D");

        _ = await Refresher(store).RefreshAsync(account, CancellationToken);

        var all = (await store.GetAllAsync(CancellationToken)).Match(accounts => accounts, _ => []);
        all.ShouldHaveSingleItem().Registration.Storage.ShouldBe(StorageUsageFactory.Create(10, 100));
    }

    public void Dispose()
    {
        services?.Dispose();
        logs.Dispose();
    }

    private async Task<AccountId> AddAccountAsync(string identifier)
    {
        var added = await store.AddAsync(AccountFixtures.Registration(identifier), CancellationToken);

        return added switch
        {
            AccountAddOutcome.Added created => created.AccountId,
            AccountAddOutcome.ReAuthenticated again => again.AccountId,
            _ => throw new InvalidOperationException("The test account must be saved."),
        };
    }

    private async Task<StorageUsage> StoredStorageAsync(AccountId account) =>
        (await store.GetAllAsync(CancellationToken)).Match(accounts => accounts.Single(stored => stored.Id == account).Registration.Storage, _ => StorageUsageFactory.Create(-1, -1));

    private IAccountQuotaRefresher Refresher(IAccountStore accountStore, bool register = true)
    {
        var collection = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(logs))
            .AddSingleton(accountStore);
        if (register)
            collection.AddKeyedSingleton<IQuotaRefresher>(CloudProviderFactory.CreateOneDrive(), provider);

        services?.Dispose();
        services = collection.AddCloudSync().BuildServiceProvider();

        return services.GetRequiredService<IAccountQuotaRefresher>();
    }
}
