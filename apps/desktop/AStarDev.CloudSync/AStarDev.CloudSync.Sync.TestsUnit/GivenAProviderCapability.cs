using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAProviderCapability : IDisposable
{
    private readonly FakeFolderBrowser oneDriveBrowser = new();
    private readonly InMemoryAccountStore accountStore = new();
    private readonly ServiceProvider provider;

    public GivenAProviderCapability() => provider = Build(accountStore);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public void when_the_provider_registered_the_capability_then_it_is_returned()
    {
        var browser = Resolver<IRemoteFolderBrowser>().ForProvider(CloudProviderFactory.CreateOneDrive());

        browser.Match(found => (object)found, error => error).ShouldBeSameAs(oneDriveBrowser);
    }

    [Fact]
    public void when_the_provider_did_not_register_the_capability_then_it_is_unsupported()
    {
        var uploader = Resolver<IRemoteFileUploader>().ForProvider(CloudProviderFactory.CreateOneDrive());

        uploader.Match(found => (object)found, error => error).ShouldBe(ProviderErrorFactory.CreateUnsupported());
    }

    [Fact]
    public async Task when_a_stored_account_asks_then_its_providers_capability_is_returned()
    {
        var accountId = await AddAccountAsync();

        var browser = await Resolver<IRemoteFolderBrowser>().ForAccountAsync(accountId, CancellationToken);

        browser.Match(found => (object)found, error => error).ShouldBeSameAs(oneDriveBrowser);
    }

    [Fact]
    public async Task when_the_account_is_unknown_then_it_is_not_found()
    {
        var browser = await Resolver<IRemoteFolderBrowser>().ForAccountAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        browser.Match(found => (object)found, error => error).ShouldBe(ProviderErrorFactory.CreateNotFound());
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_the_failure_is_transient()
    {
        using var failing = Build(new FakeAccountStore { All = () => new ErrorResponse("The accounts could not be read.") });

        var browser = await failing.GetRequiredService<IProviderCapability<IRemoteFolderBrowser>>().ForAccountAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        browser.Match(found => (object)found, error => error).ShouldBe(ProviderErrorFactory.CreateTransient());
    }

    [Fact]
    public void when_no_provider_is_given_then_the_capability_is_unsupported()
    {
        var browser = Resolver<IRemoteFolderBrowser>().ForProvider(null!);

        browser.Match(found => (object)found, error => error).ShouldBe(ProviderErrorFactory.CreateUnsupported());
    }

    [Fact]
    public void when_the_container_does_not_support_keyed_services_then_the_capability_is_unsupported()
    {
        var resolver = ResolverOver(new PlainServiceProvider());

        var browser = resolver.ForProvider(CloudProviderFactory.CreateOneDrive());

        browser.Match(found => (object)found, error => error).ShouldBe(ProviderErrorFactory.CreateUnsupported());
    }

    [Fact]
    public async Task when_the_accounts_cannot_be_read_then_a_warning_is_logged_without_details()
    {
        var logger = new RecordingLoggerProvider();
        using var failing = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(logger))
            .AddSingleton<IAccountStore>(new FakeAccountStore { All = () => new ErrorResponse("Database locked for personal@example.com") })
            .AddCloudSync()
            .BuildServiceProvider();

        _ = await failing.GetRequiredService<IProviderCapability<IRemoteFolderBrowser>>().ForAccountAsync(AccountIdFactory.Create(Guid.NewGuid()), CancellationToken);

        logger.Logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        logger.Logger.Messages.ShouldAllBe(message => !message.Contains("personal@example.com", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_many_callers_resolve_at_once_then_each_gets_the_same_capability()
    {
        var accountId = await AddAccountAsync();

        var resolved = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => Resolver<IRemoteFolderBrowser>().ForAccountAsync(accountId, CancellationToken), CancellationToken)));

        resolved.ShouldAllBe(result => result.Match(found => ReferenceEquals(found, oneDriveBrowser), _ => false));
    }

    public void Dispose() => provider.Dispose();

    private ServiceProvider Build(IAccountStore store) => new ServiceCollection()
        .AddLogging()
        .AddSingleton(store)
        .AddKeyedSingleton<IRemoteFolderBrowser>(CloudProviderFactory.CreateOneDrive(), oneDriveBrowser)
        .AddCloudSync()
        .BuildServiceProvider();

    private static IProviderCapability<IRemoteFolderBrowser> ResolverOver(IServiceProvider plain)
    {
        using var real = new ServiceCollection().AddLogging().AddSingleton<IAccountStore>(new InMemoryAccountStore()).AddCloudSync().BuildServiceProvider();
        var descriptor = new ServiceCollection().AddLogging().AddSingleton<IAccountStore>(new InMemoryAccountStore()).AddCloudSync().Single(service => service.ServiceType == typeof(IProviderCapability<>));
        var closed = descriptor.ImplementationType!.MakeGenericType(typeof(IRemoteFolderBrowser));

        return (IProviderCapability<IRemoteFolderBrowser>)ActivatorUtilities.CreateInstance(real, closed, plain);
    }

    private IProviderCapability<T> Resolver<T>()
        where T : class => provider.GetRequiredService<IProviderCapability<T>>();

    private async Task<AccountId> AddAccountAsync()
    {
        var key = ProviderAccountKeyFactory.Create("00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad").Match(value => value, _ => throw new InvalidOperationException("The test key must be valid."));
        var registration = AccountRegistrationFactory.Create(key, CloudProviderFactory.CreateOneDrive(), "Personal", "personal@example.com", new DriveId("8A3F2C1D0E9B7A65"), StorageUsageFactory.Create(1_024, 5_368_709_120));

        return (await accountStore.AddAsync(registration, CancellationToken)).ShouldBeOfType<AccountAddOutcome.Added>().AccountId;
    }
}
