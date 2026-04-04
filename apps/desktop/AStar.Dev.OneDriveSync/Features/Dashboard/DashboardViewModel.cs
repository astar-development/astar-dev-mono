using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.Sync.Engine.Features.StateTracking;
using AStar.Dev.Sync.Engine.Features.SyncOrchestration;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Dashboard;

/// <summary>
///     View model for the Dashboard view (S012).
///     Subscribes to <see cref="ISyncEngine"/> progress and state observables — no polling (NF-01, NF-02).
///     All <see cref="AStar.Dev.Functional.Extensions.Result{TSuccess,TError}"/> failures are translated
///     to observable state; no try/catch (NF-16).
/// </summary>
public sealed class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IAccountRepository _accountRepository;
    private readonly ISyncEngine _syncEngine;
    private readonly ISyncStateStore _syncStateStore;
    private readonly IDialogService _dialogService;
    private readonly IToastService _toastService;
    private readonly IRelativeTimeFormatter _timeFormatter;

    public DashboardViewModel(IAccountRepository accountRepository, ISyncEngine syncEngine, ISyncStateStore syncStateStore, IDialogService dialogService, IToastService toastService, IRelativeTimeFormatter timeFormatter)
    {
        _accountRepository = accountRepository;
        _syncEngine        = syncEngine;
        _syncStateStore    = syncStateStore;
        _dialogService     = dialogService;
        _toastService      = toastService;
        _timeFormatter     = timeFormatter;
    }

    /// <summary>All configured account cards; updated by <see cref="LoadAsync"/>.</summary>
    public ObservableCollection<AccountCardViewModel> Accounts { get; } = [];

    /// <summary>Loads accounts and checks for interrupted syncs. Must be called after construction.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var accounts = await _accountRepository.GetAllAsync(ct).ConfigureAwait(false);

        List<AccountCardViewModel> cards = [];
        foreach (var account in accounts)
        {
            var state         = await _syncStateStore.GetStateAsync(account.Id.ToString(), ct).ConfigureAwait(false);
            bool isInterrupted = state == SyncAccountState.Interrupted;
            string? lastSynced = account.LastSyncedAt.HasValue
                ? _timeFormatter.Format(account.LastSyncedAt.Value, DateTimeOffset.UtcNow)
                : null;

            var card = new AccountCardViewModel(
                accountId:      account.Id.ToString(),
                displayName:    account.DisplayName,
                isAuthRequired: account.AuthState == nameof(AccountAuthState.AuthRequired),
                lastSynced:     lastSynced,
                isInterrupted:  isInterrupted,
                isSyncActive:   account.IsSyncActive,
                onSyncNow:      (c, token) => HandleSyncNowAsync(c, token),
                onResume:       (c, token) => HandleResumeAsync(c, token));

            cards.Add(card);
        }

        RxApp.MainThreadScheduler.Schedule(() =>
        {
            DisposeCards();
            Accounts.Clear();
            foreach (var card in cards)
                Accounts.Add(card);
        });
    }

    /// <inheritdoc />
    public void Dispose() => DisposeCards();

    private void DisposeCards()
    {
        foreach (var card in Accounts)
            card.Dispose();
    }

    private async Task HandleSyncNowAsync(AccountCardViewModel card, CancellationToken ct)
    {
        var allAccounts = await _accountRepository.GetAllAsync(ct).ConfigureAwait(false);
        bool anotherIsSyncing = allAccounts.Any(a => a.IsSyncActive && a.Id.ToString() != card.AccountId);

        if (anotherIsSyncing)
        {
            bool confirmed = await _dialogService.ConfirmAsync(
                DashboardStrings.MultiAccountWarningTitle,
                DashboardStrings.MultiAccountWarningMessage,
                ct).ConfigureAwait(false);

            if (!confirmed)

                return;
        }

        await RunSyncAsync(card, isFullResync: false, ct).ConfigureAwait(false);
    }

    private async Task HandleResumeAsync(AccountCardViewModel card, CancellationToken ct)
    {
        var result = await ExecuteSyncAsync(card, isFullResync: false, ct).ConfigureAwait(false);

        await result.MatchAsync(
            onSuccess: report => ApplySuccessAsync(card, report, ct),
            onFailure: error  => ApplyResumeFailureAsync(card, error, ct)).ConfigureAwait(false);
    }

    private async Task RunSyncAsync(AccountCardViewModel card, bool isFullResync, CancellationToken ct)
    {
        var result = await ExecuteSyncAsync(card, isFullResync, ct).ConfigureAwait(false);

        await result.MatchAsync(
            onSuccess: report => ApplySuccessAsync(card, report, ct),
            onFailure: error  => ApplySyncFailureAsync(card, error, ct)).ConfigureAwait(false);
    }

    private async Task<AStar.Dev.Functional.Extensions.Result<SyncReport, SyncEngineError>> ExecuteSyncAsync(AccountCardViewModel card, bool isFullResync, CancellationToken ct)
    {
        card.IsSyncing = true;
        card.HasLocalPathError = false;
        card.SubscribeToProgress(_syncEngine.GetProgressStream(card.AccountId));

        var result = await _syncEngine.StartSyncAsync(card.AccountId, isFullResync, ct).ConfigureAwait(false);

        card.IsSyncing = false;
        card.ResetProgress();

        return result;
    }

    private async Task<bool> ApplySuccessAsync(AccountCardViewModel card, SyncReport report, CancellationToken ct)
    {
        card.LastSynced    = _timeFormatter.Format(report.CompletedAt, DateTimeOffset.UtcNow);
        card.IsInterrupted = false;

        if (report.HasSkippedFiles)
            _toastService.Show(DashboardStrings.SkippedFilesToastMessage, card.AccountId);

        return true;
    }

    private async Task<bool> ApplySyncFailureAsync(AccountCardViewModel card, SyncEngineError error, CancellationToken ct)
    {
        if (error is FullResyncRequiredError)
        {
            bool confirmed = await _dialogService.ConfirmAsync(
                DashboardStrings.FullResyncTitle,
                DashboardStrings.FullResyncMessage,
                ct).ConfigureAwait(false);

            if (confirmed)
                await RunSyncAsync(card, isFullResync: true, ct).ConfigureAwait(false);
        }
        else if (error is LocalPathUnavailableError)
        {
            card.HasLocalPathError = true;
        }

        return false;
    }

    private async Task<bool> ApplyResumeFailureAsync(AccountCardViewModel card, SyncEngineError error, CancellationToken ct)
    {
        if (error is ResumeFailedError)
        {
            bool confirmed = await _dialogService.ConfirmAsync(
                DashboardStrings.ResumeFailedTitle,
                DashboardStrings.ResumeFailedMessage,
                ct).ConfigureAwait(false);

            if (confirmed)
                await RunSyncAsync(card, isFullResync: true, ct).ConfigureAwait(false);

            return false;
        }

        return await ApplySyncFailureAsync(card, error, ct).ConfigureAwait(false);
    }
}
