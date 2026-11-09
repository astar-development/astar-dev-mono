using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class AccountServices : IDisposable
{
    private readonly ServiceProvider provider;

    public AccountServices(IAccountStore? store = null)
    {
        Store = store ?? new InMemoryAccountStore();
        SignOut = new FakeProviderSignOut(Steps);
        Cache = new FakeRemovalParticipant(Steps, "token-cache");
        Sync = new FakeRemovalParticipant(Steps, "sync-data");
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<TimeProvider>(Time)
            .AddSingleton(Store)
            .AddKeyedSingleton<IProviderSignOut>(CloudProviderFactory.CreateOneDrive(), SignOut)
            .AddKeyedSingleton<IAccountReauthenticator>(CloudProviderFactory.CreateOneDrive(), Reauthenticator)
            .AddKeyedSingleton<IAccountSignInProbe>(CloudProviderFactory.CreateOneDrive(), Probe)
            .AddSingleton<IAccountRemovalParticipant>(Cache)
            .AddSingleton<IAccountRemovalParticipant>(Sync)
            .AddCloudSync()
            .BuildServiceProvider();
    }

    public List<string> Steps { get; } = [];

    public IAccountStore Store { get; }

    public FakeProviderSignOut SignOut { get; }

    public FakeRemovalParticipant Cache { get; }

    public FakeRemovalParticipant Sync { get; }

    public FakeReauthenticator Reauthenticator { get; } = new();

    public FakeSignInProbe Probe { get; } = new();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 9, 9, 0, 0, TimeSpan.Zero));

    public RecordingLoggerProvider Logs { get; } = new();

    public IAccountActions Actions => provider.GetRequiredService<IAccountActions>();

    public IAccountSyncLock SyncLock => provider.GetRequiredService<IAccountSyncLock>();

    public IAccountSignInStates States => provider.GetRequiredService<IAccountSignInStates>();

    public ISignInStateRefresher Refresher => provider.GetRequiredService<ISignInStateRefresher>();

    public async Task<AccountId> AddAsync(string identifier, CancellationToken cancellationToken) => ((AccountAddOutcome.Added)await Store.AddAsync(AccountFixtures.Registration(identifier), cancellationToken)).AccountId;

    public async Task<T> AdvanceUntilAsync<T>(Task<T> running)
    {
        var realTime = System.Diagnostics.Stopwatch.StartNew();
        while (!running.IsCompleted && realTime.Elapsed < TimeSpan.FromSeconds(10))
        {
            Time.Advance(TimeSpan.FromMilliseconds(200));
            await Task.Delay(1);
        }

        return await running.WaitAsync(TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        provider.Dispose();
        Logs.Dispose();
    }
}
