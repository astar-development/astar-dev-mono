using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDriveSync.Features.Accounts;
using AStar.Dev.OneDriveSync.Features.Dashboard;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using AStar.Dev.Sync.Engine.Features.ProgressTracking;
using AStar.Dev.Sync.Engine.Features.StateTracking;
using AStar.Dev.Sync.Engine.Features.SyncOrchestration;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.Dashboard;

public sealed class GivenADashboardViewModel : IDisposable
{
    private static readonly Guid AccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IScheduler _originalScheduler = RxApp.MainThreadScheduler;

    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncEngine _syncEngine = Substitute.For<ISyncEngine>();
    private readonly ISyncStateStore _syncStateStore = Substitute.For<ISyncStateStore>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IToastService _toastService = Substitute.For<IToastService>();
    private readonly IRelativeTimeFormatter _timeFormatter = Substitute.For<IRelativeTimeFormatter>();

    public GivenADashboardViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;

        _accountRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([BuildAccount()]);

        _syncStateStore.GetStateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SyncAccountState?)null);

        _syncEngine.GetProgressStream(Arg.Any<string>())
            .Returns(Observable.Empty<SyncProgress>());

        _timeFormatter.Format(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
            .Returns("just now");
    }

    public void Dispose() => RxApp.MainThreadScheduler = _originalScheduler;

    [Fact]
    public async Task when_loaded_then_accounts_collection_is_populated()
    {
        var sut = await CreateAndLoadSutAsync();

        sut.Accounts.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_loaded_with_no_accounts_then_accounts_collection_is_empty()
    {
        _accountRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var sut = await CreateAndLoadSutAsync();

        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_sync_now_is_clicked_then_card_transitions_through_syncing_state()
    {
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(hadMultiAccountWarning: false, hasSkippedFiles: false)));

        var sut = await CreateAndLoadSutAsync();
        var card = sut.Accounts[0];
        var syncingHistory = new List<bool>();
        card.WhenAnyValue(c => c.IsSyncing).Subscribe(v => syncingHistory.Add(v));

        await card.SyncNowCommand.Execute().FirstAsync();

        syncingHistory.ShouldContain(true);
        syncingHistory[^1].ShouldBeFalse();
    }

    [Fact]
    public async Task when_multi_account_warning_occurs_and_user_cancels_then_sync_is_not_started()
    {
        _accountRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([BuildAccount(), BuildAccount(id: Guid.NewGuid(), isSyncActive: true)]);
        _dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].SyncNowCommand.Execute().FirstAsync();

        await _syncEngine.DidNotReceive().StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_multi_account_warning_occurs_and_user_confirms_then_sync_is_started()
    {
        _accountRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([BuildAccount(), BuildAccount(id: Guid.NewGuid(), isSyncActive: true)]);
        _dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(hadMultiAccountWarning: true, hasSkippedFiles: false)));

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].SyncNowCommand.Execute().FirstAsync();

        await _syncEngine.Received(1).StartSyncAsync(AccountId.ToString(), false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_full_resync_required_error_is_returned_then_resync_dialog_is_presented()
    {
        _syncEngine.StartSyncAsync(AccountId.ToString(), false, Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.FullResyncRequired()));
        _dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].SyncNowCommand.Execute().FirstAsync();

        await _dialogService.Received(1).ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_full_resync_required_and_user_confirms_then_full_resync_is_started()
    {
        _syncEngine.StartSyncAsync(AccountId.ToString(), false, Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.FullResyncRequired()));
        _syncEngine.StartSyncAsync(AccountId.ToString(), true, Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(false, false)));
        _dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].SyncNowCommand.Execute().FirstAsync();

        await _syncEngine.Received(1).StartSyncAsync(AccountId.ToString(), true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_progress_update_is_received_then_percent_complete_and_eta_are_updated()
    {
        var subject = new Subject<SyncProgress>();

        var sut = await CreateAndLoadSutAsync();
        var card = sut.Accounts[0];

        card.SubscribeToProgress(subject);
        subject.OnNext(SyncProgressFactory.Create(AccountId.ToString(), 45, 100, 180));

        card.PercentComplete.ShouldBe(45.0);
        card.EtaSeconds.ShouldBe(180);
    }

    [Fact]
    public async Task when_sync_completes_with_skipped_files_then_toast_is_shown()
    {
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(hadMultiAccountWarning: false, hasSkippedFiles: true)));

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].SyncNowCommand.Execute().FirstAsync();

        _toastService.Received(1).Show(Arg.Any<string>(), AccountId.ToString());
    }

    [Fact]
    public async Task when_sync_completes_with_skipped_files_then_toast_message_property_is_set()
    {
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(hadMultiAccountWarning: false, hasSkippedFiles: true)));

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].SyncNowCommand.Execute().FirstAsync();

        sut.ToastMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_dismiss_toast_command_is_executed_then_toast_message_is_cleared()
    {
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(hadMultiAccountWarning: false, hasSkippedFiles: true)));

        var sut = await CreateAndLoadSutAsync();
        await sut.Accounts[0].SyncNowCommand.Execute().FirstAsync();

        await sut.DismissToastCommand.Execute().FirstAsync();

        sut.ToastMessage.ShouldBeNull();
    }

    [Fact]
    public async Task when_local_path_unavailable_error_is_returned_then_card_shows_error_badge()
    {
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.LocalPathUnavailable()));

        var sut = await CreateAndLoadSutAsync();
        var card = sut.Accounts[0];

        await card.SyncNowCommand.Execute().FirstAsync();

        card.HasLocalPathError.ShouldBeTrue();
    }

    [Fact]
    public async Task when_local_path_error_clears_after_successful_sync_then_error_badge_is_hidden()
    {
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(
                new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.LocalPathUnavailable()),
                new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(false, false)));

        var sut = await CreateAndLoadSutAsync();
        var card = sut.Accounts[0];
        await card.SyncNowCommand.Execute().FirstAsync();
        await card.SyncNowCommand.Execute().FirstAsync();

        card.HasLocalPathError.ShouldBeFalse();
    }

    [Fact]
    public async Task when_interrupted_sync_detected_on_load_then_card_shows_interrupted_state()
    {
        _syncStateStore.GetStateAsync(AccountId.ToString(), Arg.Any<CancellationToken>())
            .Returns(SyncAccountState.Interrupted);

        var sut = await CreateAndLoadSutAsync();

        sut.Accounts[0].IsInterrupted.ShouldBeTrue();
    }

    [Fact]
    public async Task when_dismiss_interrupted_is_clicked_then_interrupted_state_is_cleared()
    {
        _syncStateStore.GetStateAsync(AccountId.ToString(), Arg.Any<CancellationToken>())
            .Returns(SyncAccountState.Interrupted);

        var sut = await CreateAndLoadSutAsync();
        var card = sut.Accounts[0];

        await card.DismissInterruptedCommand.Execute().FirstAsync();

        card.IsInterrupted.ShouldBeFalse();
    }

    [Fact]
    public async Task when_resume_command_is_executed_then_sync_is_started()
    {
        _syncStateStore.GetStateAsync(AccountId.ToString(), Arg.Any<CancellationToken>())
            .Returns(SyncAccountState.Interrupted);
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(false, false)));

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].ResumeCommand.Execute().FirstAsync();

        await _syncEngine.Received(1).StartSyncAsync(AccountId.ToString(), false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resume_fails_with_resume_failed_error_then_full_resync_dialog_is_presented()
    {
        _syncStateStore.GetStateAsync(AccountId.ToString(), Arg.Any<CancellationToken>())
            .Returns(SyncAccountState.Interrupted);
        _syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.ResumeFailed()));
        _dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].ResumeCommand.Execute().FirstAsync();

        await _dialogService.Received(1).ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resume_fails_and_user_confirms_full_resync_then_full_resync_is_started()
    {
        _syncStateStore.GetStateAsync(AccountId.ToString(), Arg.Any<CancellationToken>())
            .Returns(SyncAccountState.Interrupted);
        _syncEngine.StartSyncAsync(AccountId.ToString(), false, Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Error(SyncEngineErrorFactory.ResumeFailed()));
        _syncEngine.StartSyncAsync(AccountId.ToString(), true, Arg.Any<CancellationToken>())
            .Returns(new Result<SyncReport, SyncEngineError>.Ok(BuildSuccessReport(false, false)));
        _dialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = await CreateAndLoadSutAsync();

        await sut.Accounts[0].ResumeCommand.Execute().FirstAsync();

        await _syncEngine.Received(1).StartSyncAsync(AccountId.ToString(), true, Arg.Any<CancellationToken>());
    }

    private async Task<DashboardViewModel> CreateAndLoadSutAsync()
    {
        var sut = new DashboardViewModel(_accountRepository, _syncEngine, _syncStateStore, _dialogService, _toastService, _timeFormatter);
        await sut.LoadAsync(TestContext.Current.CancellationToken);

        return sut;
    }

    private static Account BuildAccount(Guid? id = null, bool isSyncActive = false)
        => new() { Id = id ?? AccountId, DisplayName = "Alice", Email = "alice@example.com", MicrosoftAccountId = "ms-1", IsSyncActive = isSyncActive };

    private static SyncReport BuildSuccessReport(bool hadMultiAccountWarning, bool hasSkippedFiles)
        => SyncReportFactory.Create(AccountId.ToString(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, 0, hasSkippedFiles ? 1 : 0, 0, [], false, hasSkippedFiles, hadMultiAccountWarning);
}
