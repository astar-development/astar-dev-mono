using AStarDev.CloudSync.Tray;
using AStarDev.CloudSync.ViewModels.Tray;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenAFileTrayNoticeStore
{
    private const string MarkerPath = "/home/user/.config/AStarDev.CloudSync/tray-notice-shown";

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    private FileTrayNoticeStore Create() => new(fileSystem.FileInfo.New(MarkerPath), NullLogger<FileTrayNoticeStore>.Instance);

    [Fact]
    public void when_nothing_was_recorded_then_the_notice_is_pending()
    {
        Create().Read().ShouldBe(TrayNoticeStateFactory.CreatePending());
    }

    [Fact]
    public void when_the_notice_was_marked_shown_then_a_new_store_on_the_same_file_reads_shown()
    {
        Create().MarkShown();

        Create().Read().ShouldBe(TrayNoticeStateFactory.CreateShown());
    }

    [Fact]
    public void when_the_notice_is_marked_twice_then_it_is_not_an_error()
    {
        var store = Create();

        store.MarkShown();
        store.MarkShown();

        store.Read().ShouldBe(TrayNoticeStateFactory.CreateShown());
    }

    [Fact]
    public void when_the_marker_cannot_be_written_then_nothing_is_thrown_and_the_notice_stays_pending()
    {
        _ = fileSystem.Directory.CreateDirectory("/home/user/.config");
        fileSystem.File.WriteAllText("/home/user/.config/AStarDev.CloudSync", "a file where the folder should be");

        var store = Create();
        store.MarkShown();

        store.Read().ShouldBe(TrayNoticeStateFactory.CreatePending());
    }
}
