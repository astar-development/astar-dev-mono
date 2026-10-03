using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAnIgnoredWallpaperRecorder
{
    private readonly FakeIgnoredWallpapers ignoredWallpapers = new();
    private readonly CapturingProgress progress = new();
    private readonly IgnoredWallpaperRecorder recorder;

    public GivenAnIgnoredWallpaperRecorder()
    {
        recorder = new(ignoredWallpapers);
    }

    [Fact]
    public void when_a_wallpaper_is_remembered_then_its_handle_is_recorded()
    {
        recorder.Remember("ignored-wallpaper", progress);

        ignoredWallpapers.Recorded.ShouldBe([FileHandle.Create("ignored-wallpaper")]);
        progress.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void when_recording_fails_then_the_failure_is_reported_and_not_thrown()
    {
        ignoredWallpapers.RecordResult = new InvalidOperationException("record failed");

        recorder.Remember("ignored-wallpaper", progress);

        progress.Messages.ShouldBe(["Failed to remember that wallpaper ignored-wallpaper is ignored: record failed"]);
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
