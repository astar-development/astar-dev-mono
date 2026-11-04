using AStarDev.CloudSync.Core;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestsUnit;

public class GivenThePlaceholderSyncActions
{
    [Fact]
    public async Task when_every_action_is_requested_then_each_is_logged_as_information_and_completes()
    {
        var logger = new RecordingLogger<PlaceholderSyncActions>();
        var syncActions = new PlaceholderSyncActions(logger);
        var accountId = AccountIdFactory.Create();
        var conflict = SyncConflictFactory.Create(SyncPairIdFactory.Create(), "holiday-042.jpg", TestData.Now);
        var cancellationToken = TestContext.Current.CancellationToken;

        await syncActions.SyncNowAsync(accountId, cancellationToken);
        await syncActions.CancelSyncAsync(accountId, cancellationToken);
        await syncActions.PauseAsync(accountId, cancellationToken);
        await syncActions.AddAccountAsync(cancellationToken);
        await syncActions.AddSyncPairAsync(Option.Some(accountId), cancellationToken);
        await syncActions.AddSyncPairAsync(Option.None<AccountId>(), cancellationToken);
        await syncActions.KeepBothAsync(conflict, cancellationToken);
        await syncActions.KeepLocalAsync(conflict, cancellationToken);
        await syncActions.KeepCloudAsync(conflict, cancellationToken);

        logger.Entries.Count.ShouldBe(9);
        logger.Entries.ShouldAllBe(entry => entry.Level == LogLevel.Information);
    }
}
