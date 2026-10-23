using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Infrastructure.Notifications;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Notifications;

public sealed class GivenASyncNotificationService
{
    private readonly ISyncEventAggregator aggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAppActivityProvider activityProvider = Substitute.For<IAppActivityProvider>();
    private readonly INotificationSender sender = Substitute.For<INotificationSender>();
    private readonly IAccountNameResolver nameResolver = Substitute.For<IAccountNameResolver>();
    private readonly ILocalizationService loc = Substitute.For<ILocalizationService>();

    public GivenASyncNotificationService()
    {
        _ = activityProvider.Current().Returns(AppActivityFactory.CreateBackground());
        _ = nameResolver.Resolve("acc-1").Returns("Alice");
        _ = loc.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        _ = loc.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => $"{call.Arg<string>()}|{string.Join(",", call.Arg<object[]>())}");
    }

    private SyncNotificationService CreateStartedSut()
    {
        var sut = new SyncNotificationService(aggregator, activityProvider, sender, nameResolver, loc);
        sut.Start();

        return sut;
    }

    private static SyncConflict CreateConflict(string accountId, string relativePath) => new()
    {
        Remote = RemoteItemRefFactory.Create(new AccountId(accountId), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty)),
        Target = SyncFileTargetFactory.Create(string.Empty, relativePath)
    };

    private void RaiseError(string accountId, string message) => aggregator.SyncProgressChanged += Raise.Event<EventHandler<SyncProgressEventArgs>>(aggregator, new SyncProgressEventArgs(accountId, message, SyncState.Error));

    private void RaiseCompleted(string accountId) => aggregator.SyncCompleted += Raise.Event<EventHandler<string>>(aggregator, accountId);

    [Fact]
    public void when_sync_completes_in_the_background_then_a_sync_complete_notification_is_sent()
    {
        _ = CreateStartedSut();

        RaiseCompleted("acc-1");

        _ = sender.Received(1).SendAsync(DesktopNotificationFactory.Create("Notification.SyncComplete.Title", "Notification.SyncComplete.Body|Alice"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_sync_completes_in_the_foreground_then_no_notification_is_sent()
    {
        _ = activityProvider.Current().Returns(AppActivityFactory.CreateForeground());
        _ = CreateStartedSut();

        RaiseCompleted("acc-1");

        _ = sender.DidNotReceive().SendAsync(Arg.Any<DesktopNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_conflict_is_detected_in_the_background_then_a_conflict_notification_names_the_file_and_account()
    {
        _ = CreateStartedSut();

        aggregator.ConflictDetected += Raise.Event<EventHandler<SyncConflict>>(aggregator, CreateConflict("acc-1", "Docs/report.docx"));

        _ = sender.Received(1).SendAsync(DesktopNotificationFactory.Create("Notification.Conflict.Title", "Notification.Conflict.Body|report.docx,Alice"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_conflict_is_detected_in_the_foreground_then_no_notification_is_sent()
    {
        _ = activityProvider.Current().Returns(AppActivityFactory.CreateForeground());
        _ = CreateStartedSut();

        aggregator.ConflictDetected += Raise.Event<EventHandler<SyncConflict>>(aggregator, CreateConflict("acc-1", "a.txt"));

        _ = sender.DidNotReceive().SendAsync(Arg.Any<DesktopNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_a_sync_error_is_reported_in_the_background_then_an_error_notification_is_sent()
    {
        _ = CreateStartedSut();

        RaiseError("acc-1", "Auth failed");

        _ = sender.Received(1).SendAsync(DesktopNotificationFactory.Create("Notification.SyncError.Title", "Notification.SyncError.Body|Alice,Auth failed"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_a_sync_error_is_reported_in_the_foreground_then_no_notification_is_sent()
    {
        _ = activityProvider.Current().Returns(AppActivityFactory.CreateForeground());
        _ = CreateStartedSut();

        RaiseError("acc-1", "Auth failed");

        _ = sender.DidNotReceive().SendAsync(Arg.Any<DesktopNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_a_sync_errors_more_than_once_before_completing_then_only_one_error_notification_is_sent()
    {
        _ = CreateStartedSut();

        RaiseError("acc-1", "first");
        RaiseError("acc-1", "second");

        _ = sender.Received(1).SendAsync(Arg.Any<DesktopNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_a_sync_that_errored_completes_then_no_sync_complete_notification_is_sent()
    {
        _ = CreateStartedSut();
        RaiseError("acc-1", "boom");
        sender.ClearReceivedCalls();

        RaiseCompleted("acc-1");

        _ = sender.DidNotReceive().SendAsync(Arg.Any<DesktopNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_an_errored_sync_has_completed_then_the_next_sync_can_notify_again()
    {
        _ = CreateStartedSut();
        RaiseError("acc-1", "boom");
        RaiseCompleted("acc-1");
        sender.ClearReceivedCalls();

        RaiseCompleted("acc-1");

        _ = sender.Received(1).SendAsync(Arg.Any<DesktopNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_progress_is_not_an_error_then_no_notification_is_sent()
    {
        _ = CreateStartedSut();

        aggregator.SyncProgressChanged += Raise.Event<EventHandler<SyncProgressEventArgs>>(aggregator, new SyncProgressEventArgs("acc-1", "file.txt", SyncState.Syncing));

        _ = sender.DidNotReceive().SendAsync(Arg.Any<DesktopNotification>(), Arg.Any<CancellationToken>());
    }
}
