using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;
using Shouldly;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Autostart;

public sealed class GivenAnAutostartExecutableResolver
{
    [Fact]
    public void when_appimage_path_is_set_then_it_is_returned()
    {
        var result = AutostartExecutableResolver.Resolve("/tmp/.mount_AStarDpp/usr/bin/AStarDev.OneDriveSyncClient", "/home/user/Apps/AStarDev.AppImage");

        result.ShouldBe("/home/user/Apps/AStarDev.AppImage");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_appimage_path_is_missing_then_process_path_is_returned(string? appImagePath)
    {
        var result = AutostartExecutableResolver.Resolve("/opt/astar/AStarDev.OneDriveSyncClient", appImagePath);

        result.ShouldBe("/opt/astar/AStarDev.OneDriveSyncClient");
    }

    [Fact]
    public void when_both_are_missing_then_empty_is_returned()
    {
        var result = AutostartExecutableResolver.Resolve(null, null);

        result.ShouldBeEmpty();
    }
}
