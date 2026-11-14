using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class SyncRunServices : IDisposable
{
    private readonly ServiceProvider provider;

    public SyncRunServices(InMemorySyncRunJournal? journal = null, InMemorySyncBaseline? baseline = null)
    {
        Journal = journal ?? new InMemorySyncRunJournal(Time);
        Baseline = baseline ?? new InMemorySyncBaseline();
        provider = new ServiceCollection()
            .AddLogging(logging => logging.ClearProviders().AddProvider(Logs))
            .AddSingleton<TimeProvider>(Time)
            .AddSingleton<IConfirmSyncRestartDialog>(Dialog)
            .AddSingleton<ISyncAttentionWriter>(Attention)
            .AddSingleton<ISyncAttentionReader>(Attention)
            .AddSingleton<ISyncAttentionCloser>(Attention)
            .AddSingleton<ISyncBaselineReader>(Baseline)
            .AddSingleton<IRemoteIndexReader>(Remote)
            .AddSingleton<IActivityLogWriter>(ActivityLog)
            .AddSingleton<ISyncRunJournal>(Journal)
            .AddCloudSync()
            .AddSingleton<IRemoteIndexUpdater>(Steps)
            .AddSingleton<ILocalChangeDetector>(Steps)
            .AddSingleton<IDownloadPass>(Steps)
            .AddSingleton<IUploadPass>(Steps)
            .AddSingleton<IAccountQuotaRefresher>(Quota)
            .BuildServiceProvider();
    }

    public AccountId Personal { get; } = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public AccountId Work { get; } = AccountIdFactory.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    public ScriptedSyncSteps Steps { get; } = new();

    public ScriptedQuotaRefresher Quota { get; } = new();

    public RecordingAttentionWriter Attention { get; } = new();

    public InMemorySyncBaseline Baseline { get; }

    public InMemorySyncRunJournal Journal { get; }

    public ScriptedRemoteIndex Remote { get; } = new();

    public RecordingActivityLogWriter ActivityLog { get; } = new();

    public ScriptedRestartDialog Dialog { get; } = new();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));

    public RecordingLoggerProvider Logs { get; } = new();

    public IInterruptedSyncRecovery Recovery => provider.GetRequiredService<IInterruptedSyncRecovery>();

    public ISyncPassRunner Runner => provider.GetRequiredService<ISyncPassRunner>();

    public ISyncProgressFeed Feed => provider.GetRequiredService<ISyncProgressFeed>();

    public ISyncActivitySource Activity => provider.GetRequiredService<ISyncActivitySource>();

    public ISyncCancelAction Cancel => provider.GetRequiredService<ISyncCancelAction>();

    public ISyncNowAction SyncNow => provider.GetRequiredService<ISyncNowAction>();

    public ISyncRunStatusSource Status => provider.GetRequiredService<ISyncRunStatusSource>();

    public IAccountSyncLock SyncLock => provider.GetRequiredService<IAccountSyncLock>();

    public IAccountSignInStates States => provider.GetRequiredService<IAccountSignInStates>();

    public void Dispose()
    {
        provider.Dispose();
        Logs.Dispose();
    }
}
