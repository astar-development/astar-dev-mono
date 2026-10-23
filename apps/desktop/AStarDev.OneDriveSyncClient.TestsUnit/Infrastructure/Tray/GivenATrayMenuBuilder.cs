using AStarDev.OneDriveSyncClient.Infrastructure.Tray;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Tray;

public sealed class GivenATrayMenuBuilder
{
    private readonly ILocalizationService loc = Substitute.For<ILocalizationService>();

    public GivenATrayMenuBuilder()
    {
        _ = loc.GetLocal("Tray.Open").Returns("Open");
        _ = loc.GetLocal("Tray.Quit").Returns("Quit");
        _ = loc.GetLocal("Tray.SyncNow", Arg.Any<object[]>()).Returns(call => $"Sync now: {call.Arg<object[]>()[0]}");
    }

    [Fact]
    public void when_there_are_no_accounts_then_only_open_and_quit_are_returned()
    {
        var sut = new TrayMenuBuilder(loc);

        var entries = sut.Build([]);

        entries.ShouldBe([TrayMenuEntryFactory.CreateOpen("Open"), TrayMenuEntryFactory.CreateQuit("Quit")]);
    }

    [Fact]
    public void when_there_are_accounts_then_open_first_sync_now_per_account_then_quit()
    {
        var sut = new TrayMenuBuilder(loc);
        var accounts = new[] { TrayAccountFactory.Create("id-1", "Alice"), TrayAccountFactory.Create("id-2", "Bob") };

        var entries = sut.Build(accounts);

        entries.ShouldBe(
        [
            TrayMenuEntryFactory.CreateOpen("Open"),
            TrayMenuEntryFactory.CreateSyncNow("id-1", "Sync now: Alice"),
            TrayMenuEntryFactory.CreateSyncNow("id-2", "Sync now: Bob"),
            TrayMenuEntryFactory.CreateQuit("Quit")
        ]);
    }

    [Fact]
    public void when_an_account_has_no_display_name_then_its_sync_now_label_uses_an_empty_name()
    {
        var sut = new TrayMenuBuilder(loc);

        var entries = sut.Build([TrayAccountFactory.Create("id-1", null)]);

        entries.OfType<TrayMenuEntry.SyncNow>().Single().Label.ShouldBe("Sync now: ");
    }
}
