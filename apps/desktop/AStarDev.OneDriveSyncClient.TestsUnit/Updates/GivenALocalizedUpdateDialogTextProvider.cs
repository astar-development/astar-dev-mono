using System.Globalization;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Updates;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Updates;

public sealed class GivenALocalizedUpdateDialogTextProvider
{
    private static (LocalizedUpdateDialogTextProvider Sut, ILocalizationService Localization) CreateSut()
    {
        var localization = Substitute.For<ILocalizationService>();
        var sut = new LocalizedUpdateDialogTextProvider(localization);

        return (sut, localization);
    }

    [Fact]
    public void when_title_is_read_then_it_forwards_to_localization_service_with_the_update_title_key()
    {
        var (sut, localization) = CreateSut();
        localization.GetLocal("Update.Title").Returns("Update available");

        sut.Title.ShouldBe("Update available");
    }

    [Fact]
    public void when_release_notes_label_is_read_then_it_forwards_to_localization_service_with_the_release_notes_key()
    {
        var (sut, localization) = CreateSut();
        localization.GetLocal("Update.ReleaseNotesLabel").Returns("What's new");

        sut.ReleaseNotesLabel.ShouldBe("What's new");
    }

    [Fact]
    public void when_restart_now_label_is_read_then_it_forwards_to_localization_service_with_the_restart_now_key()
    {
        var (sut, localization) = CreateSut();
        localization.GetLocal("Update.RestartNow").Returns("Restart now");

        sut.RestartNowLabel.ShouldBe("Restart now");
    }

    [Fact]
    public void when_later_label_is_read_then_it_forwards_to_localization_service_with_the_later_key()
    {
        var (sut, localization) = CreateSut();
        localization.GetLocal("Update.Later").Returns("Later");

        sut.LaterLabel.ShouldBe("Later");
    }

    [Fact]
    public void when_downloading_label_is_read_then_it_forwards_to_localization_service_with_the_downloading_key()
    {
        var (sut, localization) = CreateSut();
        localization.GetLocal("Update.Downloading").Returns("Downloading...");

        sut.DownloadingLabel.ShouldBe("Downloading...");
    }

    [Fact]
    public void when_get_message_is_called_then_it_forwards_to_localization_service_with_the_update_message_key_and_version()
    {
        var (sut, localization) = CreateSut();
        localization.GetLocal("Update.Message", "1.2.3").Returns("Version 1.2.3 is ready to install.");

        sut.GetMessage("1.2.3").ShouldBe("Version 1.2.3 is ready to install.");
    }

    [Fact]
    public void when_culture_changed_is_raised_then_text_changed_is_raised()
    {
        var (sut, localization) = CreateSut();
        bool textChanged = false;
        sut.TextChanged += (_, _) => textChanged = true;

        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, CultureInfo.InvariantCulture);

        textChanged.ShouldBeTrue();
    }

    [Fact]
    public void when_disposed_then_it_unsubscribes_from_culture_changed()
    {
        var (sut, localization) = CreateSut();
        bool textChanged = false;
        sut.TextChanged += (_, _) => textChanged = true;

        sut.Dispose();
        localization.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(localization, CultureInfo.InvariantCulture);

        textChanged.ShouldBeFalse();
    }
}
