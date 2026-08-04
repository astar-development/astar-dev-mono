
using AStarDev.WallpaperScraper.Startup;

namespace AStarDev.WallpaperScraper.Tests.Unit;

public sealed class GivenAPlainUpdateDialogTextProvider
{
    private static PlainUpdateDialogTextProvider CreateSut() => new();

    [Fact]
    public void when_title_is_read_then_it_returns_update_available() => CreateSut().Title.ShouldBe("Update available");

    [Fact]
    public void when_release_notes_label_is_read_then_it_returns_whats_new() => CreateSut().ReleaseNotesLabel.ShouldBe("What's new");

    [Fact]
    public void when_restart_now_label_is_read_then_it_returns_restart_now() => CreateSut().RestartNowLabel.ShouldBe("Restart now");

    [Fact]
    public void when_later_label_is_read_then_it_returns_later() => CreateSut().LaterLabel.ShouldBe("Later");

    [Fact]
    public void when_downloading_label_is_read_then_it_returns_downloading() => CreateSut().DownloadingLabel.ShouldBe("Downloading...");

    [Fact]
    public void when_get_message_is_called_then_it_returns_the_version_formatted_message() => CreateSut().GetMessage("1.2.3").ShouldBe("Version 1.2.3 is ready to install.");
}
