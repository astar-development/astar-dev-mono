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
    [Fact]
    public async Task when_every_action_except_add_account_and_add_sync_pair_is_requested_then_each_is_logged_as_information_and_completes()
    {
        var logger = new RecordingLogger<PlaceholderSyncActions>();
        var syncActions = new PlaceholderSyncActions(new RecordingAddAccountDialog(), new RecordingFolderSelectionDialog(), logger);
        var accountId = AccountIdFactory.Create(Guid.NewGuid());
        var conflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), "holiday-042.jpg", TestData.Now);
        var cancellationToken = TestContext.Current.CancellationToken;

        await syncActions.SyncNowAsync(accountId, cancellationToken);
        await syncActions.CancelSyncAsync(accountId, cancellationToken);
        await syncActions.PauseAsync(accountId, cancellationToken);
        await syncActions.KeepBothAsync(conflict, cancellationToken);
        await syncActions.KeepLocalAsync(conflict, cancellationToken);
        await syncActions.KeepCloudAsync(conflict, cancellationToken);

        logger.Entries.Count.ShouldBe(6);
        logger.Entries.ShouldAllBe(entry => entry.Level == LogLevel.Information);
    }

    [Fact]
    public async Task when_add_account_is_requested_then_the_add_account_dialog_is_shown_with_the_callers_token()
    {
        var dialog = new RecordingAddAccountDialog();
        using var cancellation = new CancellationTokenSource();

        await new PlaceholderSyncActions(dialog, new RecordingFolderSelectionDialog(), new RecordingLogger<PlaceholderSyncActions>()).AddAccountAsync(cancellation.Token);

        dialog.ReceivedTokens.ShouldBe([cancellation.Token]);
    }

    [Fact]
    public async Task when_add_sync_pair_is_requested_for_an_account_then_the_folder_selection_dialog_is_shown_for_it_with_the_callers_token()
    {
        var dialog = new RecordingFolderSelectionDialog();
        var accountId = AccountIdFactory.Create(Guid.NewGuid());
        using var cancellation = new CancellationTokenSource();

        await new PlaceholderSyncActions(new RecordingAddAccountDialog(), dialog, new RecordingLogger<PlaceholderSyncActions>()).AddSyncPairAsync(Option.Some(accountId), cancellation.Token);

        dialog.Shown.ShouldBe([(Option.Some(accountId), cancellation.Token)]);
    }

    [Fact]
    public async Task when_add_sync_pair_is_requested_without_an_account_then_the_dialog_is_shown_to_let_the_user_choose()
    {
        var dialog = new RecordingFolderSelectionDialog();

        await new PlaceholderSyncActions(new RecordingAddAccountDialog(), dialog, new RecordingLogger<PlaceholderSyncActions>()).AddSyncPairAsync(Option.None<AccountId>(), TestContext.Current.CancellationToken);

        dialog.Shown.Single().AccountId.Match(_ => "account", () => "none").ShouldBe("none");
    }
}
