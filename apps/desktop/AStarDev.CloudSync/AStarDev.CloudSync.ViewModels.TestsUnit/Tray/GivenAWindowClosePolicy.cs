using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Tray;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Tray;

public sealed class GivenAWindowClosePolicy
{
    private readonly RecordingTrayHost host = new();
    private readonly RecordingTrayNoticeStore store = new(TrayNoticeStateFactory.CreatePending());
    private readonly Notifications.ScriptedNotificationHost notifications = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private WindowClosePolicy Create(TrayAvailability availability) => new(host, new FixedTrayAvailability(availability), new TrayCloseNotice(store, notifications, TestData.CreateLocalization()));

    [Fact]
    public async Task when_the_tray_is_available_then_closing_hides_the_window_and_tells_the_user_once()
    {
        await Create(TrayAvailabilityFactory.CreateAvailable()).CloseAsync(CancellationToken);

        (host.Hidden, host.Minimised, notifications.Sent.Count).ShouldBe((1, 0, 1));
    }

    [Fact]
    public async Task when_the_tray_is_missing_then_closing_minimises_the_window_and_never_hides_it()
    {
        await Create(TrayAvailabilityFactory.CreateMissing()).CloseAsync(CancellationToken);

        (host.Hidden, host.Minimised).ShouldBe((0, 1));
    }

    [Fact]
    public async Task when_the_tray_is_missing_then_the_still_running_in_the_tray_notice_is_not_sent_or_recorded()
    {
        await Create(TrayAvailabilityFactory.CreateMissing()).CloseAsync(CancellationToken);

        (notifications.Sent.Count, store.Marked).ShouldBe((0, 0));
    }

    [Fact]
    public async Task when_the_window_is_closed_twice_with_a_tray_then_the_notice_is_sent_only_once_but_it_hides_both_times()
    {
        var policy = Create(TrayAvailabilityFactory.CreateAvailable());

        await policy.CloseAsync(CancellationToken);
        await policy.CloseAsync(CancellationToken);

        (host.Hidden, notifications.Sent.Count).ShouldBe((2, 1));
    }

    private sealed class FixedTrayAvailability(TrayAvailability availability) : ITrayAvailability
    {
        public TrayAvailability Check() => availability;
    }
}
