using AStarDev.CloudSync.Autostart;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenAnAutostartLauncherPath
{
    private const string MountedPath = "/tmp/.mount_CloudXYZ/usr/bin/AStarDev.CloudSync";
    private const string AppImage = "/home/test/Apps/AStarDev.CloudSync.AppImage";

    private static string Shown(Option<string> path) => path.Match(value => value, () => "none");

    [Fact]
    public void when_the_app_runs_from_an_appimage_then_the_appimage_file_is_the_path()
    {
        Shown(AutostartLauncherPath.Resolve(true, MountedPath, AppImage)).ShouldBe(AppImage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_there_is_no_appimage_then_the_process_path_is_the_path(string? appImage)
    {
        Shown(AutostartLauncherPath.Resolve(true, "/opt/cloudsync/CloudSync", appImage)).ShouldBe("/opt/cloudsync/CloudSync");
    }

    [Fact]
    public void when_the_build_is_not_installed_then_there_is_no_path_even_with_an_appimage()
    {
        Shown(AutostartLauncherPath.Resolve(false, MountedPath, AppImage)).ShouldBe("none");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void when_there_is_no_appimage_and_no_process_path_then_there_is_no_path(string? processPath)
    {
        Shown(AutostartLauncherPath.Resolve(true, processPath, null)).ShouldBe("none");
    }
}
