using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Domain.Conflicts;
using AStarDev.CloudSync.Domain.SyncPairs;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenThePlaceholderSyncActions
{
    [Fact]
    public async Task when_every_action_except_add_account_is_requested_then_each_is_logged_as_information_and_completes()
    {
        var logger = new RecordingLogger<PlaceholderSyncActions>();
        var syncActions = new PlaceholderSyncActions(new RecordingAddAccountDialog(), logger);
        var accountId = AccountIdFactory.Create(Guid.NewGuid());
        var conflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), "holiday-042.jpg", TestData.Now);
        var cancellationToken = TestContext.Current.CancellationToken;

        await syncActions.SyncNowAsync(accountId, cancellationToken);
        await syncActions.CancelSyncAsync(accountId, cancellationToken);
        await syncActions.PauseAsync(accountId, cancellationToken);
        await syncActions.AddSyncPairAsync(Option.Some(accountId), cancellationToken);
        await syncActions.AddSyncPairAsync(Option.None<AccountId>(), cancellationToken);
        await syncActions.KeepBothAsync(conflict, cancellationToken);
        await syncActions.KeepLocalAsync(conflict, cancellationToken);
        await syncActions.KeepCloudAsync(conflict, cancellationToken);

        logger.Entries.Count.ShouldBe(8);
        logger.Entries.ShouldAllBe(entry => entry.Level == LogLevel.Information);
    }

    [Fact]
    public async Task when_add_account_is_requested_then_the_add_account_dialog_is_shown_with_the_callers_token()
    {
        var dialog = new RecordingAddAccountDialog();
        using var cancellation = new CancellationTokenSource();

        await new PlaceholderSyncActions(dialog, new RecordingLogger<PlaceholderSyncActions>()).AddAccountAsync(cancellation.Token);

        dialog.ReceivedTokens.ShouldBe([cancellation.Token]);
    }
}
