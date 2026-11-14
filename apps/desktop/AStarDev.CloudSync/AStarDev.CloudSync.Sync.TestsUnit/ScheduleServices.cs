using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScheduleServices : IDisposable
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private readonly ServiceProvider provider;
    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
    private readonly List<AccountSyncRoot> roots = [];
    private int added;

    public ScheduleServices()
    {
        Time = new FakeTimeProvider(Start);
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<System.TimeProvider>(Time)
            .AddSingleton<IAccountStore>(Store)
            .AddSingleton<ISyncScheduleSettings>(Settings)
            .AddSingleton<IAccountPauseReader>(Paused)
            .AddSingleton<ISyncRootReader>(Roots)
            .AddSingleton<ISyncActivitySource>(Activity)
            .AddSingleton<ISyncPassRunner>(Runner)
            .AddCloudSync()
            .AddSingleton<ISyncPassRunner>(Runner)
            .AddSingleton<ISyncActivitySource>(Activity)
            .AddSingleton<ISyncRootReader>(Roots)
            .AddSingleton<IAccountStore>(Store)
            .AddSingleton<ISyncScheduleSettings>(Settings)
            .AddSingleton<IAccountPauseReader>(Paused)
            .BuildServiceProvider();
    }

    public static DateTimeOffset Start { get; } = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    public static TimeSpan CheckEvery { get; } = SyncSchedulerOptionsFactory.CreateDefault().CheckEvery;

    public FakeTimeProvider Time { get; }

    public RecordingLoggerProvider Logs { get; } = new();

    public InMemoryAccountStore Store { get; } = new();

    public ScheduleSettings Settings { get; } = new();

    public FakeAccountPauseReader Paused { get; } = new();

    public FakeSyncRootReader Roots { get; } = new();

    public ScheduleActivity Activity { get; } = new();

    public RecordingRunner Runner { get; } = new();

    public ISyncScheduler Scheduler => provider.GetRequiredService<ISyncScheduler>();

    public IAccountSignInStates SignIn => provider.GetRequiredService<IAccountSignInStates>();

    public async Task<AccountId> AddAccountAsync()
    {
        var number = ++added;
        var outcome = await Store.AddAsync(AccountFixtures.Registration($"account-{number}"), TestContext.Current.CancellationToken);
        var accountId = outcome is AccountAddOutcome.Added addedAccount ? addedAccount.AccountId : throw new InvalidOperationException("The test account must be added.");
        var directory = fileSystem.DirectoryInfo.New($"/sync/account-{number}");
        directory.Create();
        roots.Add(AccountSyncRootFactory.Create(accountId, SyncRootFactory.Create(directory).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid."))));
        Roots.Roots = [.. roots];

        return accountId;
    }

    public Task RemoveAccountAsync(AccountId accountId) => Store.RemoveAsync(accountId, TestContext.Current.CancellationToken);

    public void DeleteRootFolder(AccountId accountId) => roots.Single(entry => entry.AccountId == accountId).Root.Directory.Delete();

    public void ForgetRoot(AccountId accountId)
    {
        _ = roots.RemoveAll(entry => entry.AccountId == accountId);
        Roots.Roots = [.. roots];
    }

    public async Task TickAtAsync(TimeSpan sinceStart)
    {
        Time.SetUtcNow(Start + sinceStart - CheckEvery);
        Time.Advance(CheckEvery);
        await Task.Yield();
    }

    public async Task WaitForRunsAsync(int count)
    {
        using var source = new CancellationTokenSource(Limit);
        while (Runner.Runs.Count < count)
            await Task.Delay(5, source.Token);
    }

    public async Task WaitForLogAsync(string text)
    {
        using var source = new CancellationTokenSource(Limit);
        while (!Logs.Logger.Messages.Any(message => message.Contains(text)))
            await Task.Delay(5, source.Token);
    }

    public static Task NothingElseHappensAsync() => Task.Delay(100, TestContext.Current.CancellationToken);

    public void Dispose()
    {
        provider.Dispose();
        Logs.Dispose();
    }
}
