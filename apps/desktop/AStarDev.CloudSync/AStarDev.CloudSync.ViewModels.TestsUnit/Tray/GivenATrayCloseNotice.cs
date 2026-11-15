using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Tray;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

public sealed class GivenATrayCloseNotice
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_notice_has_not_been_shown_then_it_is_sent_once_and_recorded()
    {
        var store = new RecordingTrayNoticeStore(TrayNoticeStateFactory.CreatePending());
        var host = new Notifications.ScriptedNotificationHost();
        var notice = new TrayCloseNotice(store, host, TestData.CreateLocalization());

        await notice.ShowAsync(CancellationToken);

        host.Sent.Select(message => (message.Title, message.Body)).ShouldBe([("AStar Dev CloudSync", "CloudSync is still running in the tray.")]);
        store.Marked.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_notice_was_shown_before_then_nothing_is_sent()
    {
        var store = new RecordingTrayNoticeStore(TrayNoticeStateFactory.CreateShown());
        var host = new Notifications.ScriptedNotificationHost();

        await new TrayCloseNotice(store, host, TestData.CreateLocalization()).ShowAsync(CancellationToken);

        (host.Sent.Count, store.Marked).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_the_window_is_closed_twice_then_the_notice_is_sent_only_the_first_time()
    {
        var store = new RecordingTrayNoticeStore(TrayNoticeStateFactory.CreatePending());
        var host = new Notifications.ScriptedNotificationHost();
        var notice = new TrayCloseNotice(store, host, TestData.CreateLocalization());

        await notice.ShowAsync(CancellationToken);
        await notice.ShowAsync(CancellationToken);

        host.Sent.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_sender_fails_then_the_notice_stays_pending_so_the_next_close_tries_again()
    {
        var store = new RecordingTrayNoticeStore(TrayNoticeStateFactory.CreatePending());
        var host = new Notifications.ScriptedNotificationHost { Outcome = Result.Failure<Unit, NotificationFailure>(NotificationFailureFactory.CreateNotSupported()) };
        var notice = new TrayCloseNotice(store, host, TestData.CreateLocalization());

        await notice.ShowAsync(CancellationToken);
        host.Outcome = Result.Success<Unit, NotificationFailure>(Unit.Instance);
        await notice.ShowAsync(CancellationToken);

        (host.Sent.Count, store.Marked, store.State).ShouldBe((2, 1, TrayNoticeStateFactory.CreateShown()));
    }
}
