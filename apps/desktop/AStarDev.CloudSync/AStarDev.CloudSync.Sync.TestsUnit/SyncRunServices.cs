using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class SyncRunServices : IDisposable
{
    private readonly ServiceProvider provider;

    public SyncRunServices() =>
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<TimeProvider>(Time)
            .AddCloudSync()
            .AddSingleton<IRemoteIndexUpdater>(Steps)
            .AddSingleton<ILocalChangeDetector>(Steps)
            .AddSingleton<IDownloadPass>(Steps)
            .AddSingleton<IUploadPass>(Steps)
            .BuildServiceProvider();

    public AccountId Personal { get; } = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public AccountId Work { get; } = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    public ScriptedSyncSteps Steps { get; } = new();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));

    public RecordingLoggerProvider Logs { get; } = new();

    public ISyncPassRunner Runner => provider.GetRequiredService<ISyncPassRunner>();

    public ISyncActivitySource Activity => provider.GetRequiredService<ISyncActivitySource>();

    public ISyncCancelAction Cancel => provider.GetRequiredService<ISyncCancelAction>();

    public ISyncRunStatusSource Status => provider.GetRequiredService<ISyncRunStatusSource>();

    public IAccountSyncLock SyncLock => provider.GetRequiredService<IAccountSyncLock>();

    public IAccountSignInStates States => provider.GetRequiredService<IAccountSignInStates>();

    public void Dispose()
    {
        provider.Dispose();
        Logs.Dispose();
    }
}
