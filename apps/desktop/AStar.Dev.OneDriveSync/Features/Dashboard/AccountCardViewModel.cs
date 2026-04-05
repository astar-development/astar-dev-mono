using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.Sync.Engine.Features.ProgressTracking;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Dashboard;

/// <summary>
///     View model for a single account card on the Dashboard (S012, SE-03, SE-13, SE-14, AM-11, EH-05, EH-06).
/// </summary>
public sealed class AccountCardViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _progressSubscription = [];
    private readonly Func<AccountCardViewModel, CancellationToken, Task> _onSyncNow;
    private readonly Func<AccountCardViewModel, CancellationToken, Task> _onResume;

    public AccountCardViewModel(string accountId, string displayName, bool isAuthRequired, string? lastSynced, bool isInterrupted, bool isSyncActive, Func<AccountCardViewModel, CancellationToken, Task> onSyncNow, Func<AccountCardViewModel, CancellationToken, Task> onResume)
    {
        _onSyncNow = onSyncNow;
        _onResume  = onResume;

        AccountId      = accountId;
        DisplayName    = displayName;
        IsAuthRequired = isAuthRequired;
        LastSynced     = lastSynced;
        IsInterrupted  = isInterrupted;
        IsSyncing      = isSyncActive;

        SyncNowCommand         = ReactiveCommand.CreateFromTask(ct => _onSyncNow(this, ct));
        ResumeCommand          = ReactiveCommand.CreateFromTask(ct => _onResume(this, ct));
        DismissInterruptedCommand = ReactiveCommand.Create(DismissInterrupted);
    }

    /// <summary>Opaque account identifier — the account's <see cref="Guid"/> formatted as a string.</summary>
    public string AccountId { get; }

    /// <summary>Human-readable display name.</summary>
    public string DisplayName { get; }

    /// <summary>True when re-authentication is required.</summary>
    public bool IsAuthRequired
    {
        get;
        internal set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Relative or absolute last-synced timestamp; null when never synced.</summary>
    public string? LastSynced
    {
        get;
        internal set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>True while a sync is actively running for this account (SE-06).</summary>
    public bool IsSyncing
    {
        get;
        internal set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Current sync progress percentage (0–100); meaningful only when <see cref="IsSyncing"/> is true (SE-13).</summary>
    public double PercentComplete
    {
        get;
        internal set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Estimated remaining seconds; meaningful only when <see cref="IsSyncing"/> is true (SE-14).</summary>
    public int EtaSeconds
    {
        get;
        internal set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(EtaDisplay));
        }
    }

    /// <summary>Human-readable ETA string derived from <see cref="EtaSeconds"/> (SE-14).</summary>
    public string EtaDisplay => EtaSeconds switch
    {
        <= 0   => string.Empty,
        < 60   => $"ETA {EtaSeconds}s",
        _      => $"ETA {EtaSeconds / 60}m {EtaSeconds % 60}s"
    };

    /// <summary>True when the last sync failed because the local path was unavailable (AM-11).</summary>
    public bool HasLocalPathError
    {
        get;
        internal set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>True when an interrupted sync was detected for this account on startup (EH-05).</summary>
    public bool IsInterrupted
    {
        get;
        internal set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Triggers a manual sync for this account.</summary>
    public ReactiveCommand<Unit, Unit> SyncNowCommand { get; }

    /// <summary>Resumes an interrupted sync for this account (EH-05).</summary>
    public ReactiveCommand<Unit, Unit> ResumeCommand { get; }

    /// <summary>Dismisses the interrupted-sync banner without resuming (EH-05).</summary>
    public ReactiveCommand<Unit, Unit> DismissInterruptedCommand { get; }

    /// <summary>
    ///     Attaches a progress observable scoped to the current sync lifetime.
    ///     Any prior subscription is disposed first to prevent stale progress from a previous sync.
    /// </summary>
    internal void SubscribeToProgress(IObservable<SyncProgress> progressStream)
    {
        _progressSubscription.Clear();

        progressStream
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ApplyProgress)
            .DisposeWith(_progressSubscription);
    }

    internal void ResetProgress()
    {
        _progressSubscription.Clear();
        PercentComplete = 0;
        EtaSeconds      = 0;
    }

    private void ApplyProgress(SyncProgress progress)
    {
        PercentComplete = progress.PercentComplete;
        EtaSeconds      = progress.EtaSeconds;
    }

    private void DismissInterrupted() => IsInterrupted = false;

    /// <inheritdoc />
    public void Dispose()
    {
        _progressSubscription.Dispose();
        SyncNowCommand.Dispose();
        ResumeCommand.Dispose();
        DismissInterruptedCommand.Dispose();
    }
}
