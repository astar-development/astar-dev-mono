using AStarDev.CloudSync.Autostart;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenADesktopEntryBuilder
{
    [Fact]
    public void when_an_entry_is_built_then_it_has_the_name_the_command_and_the_minimised_flag()
    {
        var entry = DesktopEntryBuilder.Build("/opt/cloudsync/CloudSync");

        entry.ShouldBe("[Desktop Entry]\nType=Application\nName=AStar Dev CloudSync\nExec=\"/opt/cloudsync/CloudSync\" --minimized\nX-GNOME-Autostart-Delay=30\nTerminal=false\n");
    }

    [Theory]
    [InlineData("/opt/my apps/CloudSync", "Exec=\"/opt/my apps/CloudSync\" --minimized")]
    [InlineData("/opt/a\"b/CloudSync", "Exec=\"/opt/a\\\\\"b/CloudSync\" --minimized")]
    [InlineData("/opt/%u/CloudSync", "Exec=\"/opt/%%u/CloudSync\" --minimized")]
    [InlineData("/opt/$HOME/CloudSync", "Exec=\"/opt/\\\\$HOME/CloudSync\" --minimized")]
    public void when_the_path_has_special_characters_then_they_are_escaped_for_the_exec_line(string path, string expectedLine)
    {
        DesktopEntryBuilder.Build(path).ShouldContain(expectedLine);
    }

    [Fact]
    public void when_the_path_has_a_line_break_then_it_cannot_start_a_new_key()
    {
        var entry = DesktopEntryBuilder.Build("/opt/x\nExec=/bin/evil");

        entry.Split('\n').Count(line => line.StartsWith("Exec=", StringComparison.Ordinal)).ShouldBe(1);
    }
}
