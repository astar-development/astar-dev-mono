using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenThePlaceholderSyncActions
{
    private static PlaceholderSyncActions Create(RecordingSyncNowAction? syncNow = null, FakeAccountPauseWriter? pauseWriter = null, RecordingLogger<PlaceholderSyncActions>? logger = null)
    {
        var runs = syncNow ?? new RecordingSyncNowAction();

        return new PlaceholderSyncActions(new RecordingAddAccountDialog(), new RecordingFolderSelectionDialog(), runs, runs, pauseWriter ?? new FakeAccountPauseWriter(), logger ?? new RecordingLogger<PlaceholderSyncActions>());
    }

    [Fact]
    public async Task when_sync_now_is_requested_then_the_sync_now_action_runs_for_the_account_with_the_callers_token()
    {
        var syncNow = new RecordingSyncNowAction();
        var accountId = AccountIdFactory.Create(Guid.NewGuid());
        using var cancellation = new CancellationTokenSource();

        await Create(syncNow).SyncNowAsync(accountId, cancellation.Token);

        syncNow.SyncNowCalls.ShouldBe([(accountId, cancellation.Token)]);
    }

    [Fact]
    public async Task when_cancel_is_requested_then_the_cancel_action_runs_for_the_account_with_the_callers_token()
    {
        var syncNow = new RecordingSyncNowAction();
        var accountId = AccountIdFactory.Create(Guid.NewGuid());
        using var cancellation = new CancellationTokenSource();

        await Create(syncNow).CancelSyncAsync(accountId, cancellation.Token);

        syncNow.CancelCalls.ShouldBe([(accountId, cancellation.Token)]);
    }

    [Fact]
    public async Task when_pause_is_requested_then_the_account_is_toggled_and_the_new_state_is_logged_as_information()
    {
        var writer = new FakeAccountPauseWriter();
        var logger = new RecordingLogger<PlaceholderSyncActions>();
        var accountId = AccountIdFactory.Create(Guid.NewGuid());

        await Create(pauseWriter: writer, logger: logger).PauseAsync(accountId, TestContext.Current.CancellationToken);

        writer.Toggled.ShouldBe([accountId]);
        logger.Entries.Single().Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task when_the_pause_cannot_be_saved_then_the_request_completes_and_a_warning_is_logged_without_the_error_text()
    {
        var writer = new FakeAccountPauseWriter { Outcome = Result.Failure<PauseState, ErrorResponse>(new ErrorResponse("C:/secret/path could not be written")) };
        var logger = new RecordingLogger<PlaceholderSyncActions>();

        await Create(pauseWriter: writer, logger: logger).PauseAsync(AccountIdFactory.Create(Guid.NewGuid()), TestContext.Current.CancellationToken);

        logger.Entries.Single().Level.ShouldBe(LogLevel.Warning);
        logger.Messages.ShouldNotContain(message => message.Contains("secret"));
    }

    [Fact]
    public async Task when_a_conflict_is_resolved_then_each_choice_is_logged_as_information_and_completes()
    {
        var logger = new RecordingLogger<PlaceholderSyncActions>();
        var syncActions = Create(logger: logger);
        var conflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), "holiday-042.jpg", TestData.Now);
        var cancellationToken = TestContext.Current.CancellationToken;

        await syncActions.KeepBothAsync(conflict, cancellationToken);
        await syncActions.KeepLocalAsync(conflict, cancellationToken);
        await syncActions.KeepCloudAsync(conflict, cancellationToken);

        logger.Entries.Count.ShouldBe(3);
        logger.Entries.ShouldAllBe(entry => entry.Level == LogLevel.Information);
    }

    [Fact]
    public async Task when_add_account_is_requested_then_the_add_account_dialog_is_shown_with_the_callers_token()
    {
        var dialog = new RecordingAddAccountDialog();
        using var cancellation = new CancellationTokenSource();

        await new PlaceholderSyncActions(dialog, new RecordingFolderSelectionDialog(), new RecordingSyncNowAction(), new RecordingSyncNowAction(), new FakeAccountPauseWriter(), new RecordingLogger<PlaceholderSyncActions>()).AddAccountAsync(cancellation.Token);

        dialog.ReceivedTokens.ShouldBe([cancellation.Token]);
    }

    [Fact]
    public async Task when_add_sync_pair_is_requested_for_an_account_then_the_folder_selection_dialog_is_shown_for_it_with_the_callers_token()
    {
        var dialog = new RecordingFolderSelectionDialog();
        var accountId = AccountIdFactory.Create(Guid.NewGuid());
        using var cancellation = new CancellationTokenSource();

        await new PlaceholderSyncActions(new RecordingAddAccountDialog(), dialog, new RecordingSyncNowAction(), new RecordingSyncNowAction(), new FakeAccountPauseWriter(), new RecordingLogger<PlaceholderSyncActions>()).AddSyncPairAsync(Option.Some(accountId), cancellation.Token);

        dialog.Shown.ShouldBe([(Option.Some(accountId), cancellation.Token)]);
    }

    [Fact]
    public async Task when_add_sync_pair_is_requested_without_an_account_then_the_dialog_is_shown_to_let_the_user_choose()
    {
        var dialog = new RecordingFolderSelectionDialog();

        await new PlaceholderSyncActions(new RecordingAddAccountDialog(), dialog, new RecordingSyncNowAction(), new RecordingSyncNowAction(), new FakeAccountPauseWriter(), new RecordingLogger<PlaceholderSyncActions>()).AddSyncPairAsync(Option.None<AccountId>(), TestContext.Current.CancellationToken);

        dialog.Shown.Single().AccountId.Match(_ => "account", () => "none").ShouldBe("none");
    }
}
