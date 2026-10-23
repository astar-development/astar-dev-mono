using AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Autostart;

public sealed class GivenAnAutostartEntryBuilder
{
    [Fact]
    public void when_building_a_desktop_entry_then_it_launches_the_quoted_executable_after_the_delay()
    {
        var entry = AutostartEntryBuilder.BuildDesktopEntry("/opt/astar/app", 30);

        var lines = entry.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        lines.ShouldBe(
        [
            "[Desktop Entry]",
            "Type=Application",
            "Name=AStar OneDrive Sync",
            "Exec=\"/opt/astar/app\"",
            "Terminal=false",
            "X-GNOME-Autostart-enabled=true",
            "X-GNOME-Autostart-Delay=30"
        ]);
    }

    [Fact]
    public void when_the_desktop_entry_path_contains_special_characters_then_they_are_escaped_in_exec()
    {
        var entry = AutostartEntryBuilder.BuildDesktopEntry("/opt/my \"app\"/$HOME/a`b\\c", 5);

        entry.ShouldContain("Exec=\"/opt/my \\\"app\\\"/\\$HOME/a\\`b\\\\c\"");
    }

    [Fact]
    public void when_building_a_launch_agent_then_it_runs_the_executable_at_load_with_the_label()
    {
        var plist = AutostartEntryBuilder.BuildLaunchAgent("com.astar.onedrive-sync", "/Applications/AStar.app/Contents/MacOS/app");

        plist.ShouldContain("<key>Label</key>");
        plist.ShouldContain("<string>com.astar.onedrive-sync</string>");
        plist.ShouldContain("<string>/Applications/AStar.app/Contents/MacOS/app</string>");
        plist.ShouldContain("<key>RunAtLoad</key>");
        plist.ShouldContain("<true/>");
    }

    [Fact]
    public void when_the_launch_agent_path_contains_xml_characters_then_they_are_escaped()
    {
        var plist = AutostartEntryBuilder.BuildLaunchAgent("label", "/a&b/<app>");

        plist.ShouldContain("<string>/a&amp;b/&lt;app&gt;</string>");
    }

    [Fact]
    public void when_building_a_windows_command_then_the_path_is_quoted()
    {
        var command = AutostartEntryBuilder.BuildWindowsCommand("C:\\Users\\me\\AppData\\Local\\AStar\\current\\app.exe");

        command.ShouldBe("\"C:\\Users\\me\\AppData\\Local\\AStar\\current\\app.exe\"");
    }
}
