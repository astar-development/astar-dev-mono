using AStarDev.CloudSync.Autostart;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenALaunchAgentPlistBuilder
{
    [Fact]
    public void when_a_plist_is_built_then_it_has_the_label_the_command_and_run_at_load()
    {
        var plist = LaunchAgentPlistBuilder.Build("/Applications/CloudSync.app/Contents/MacOS/CloudSync");

        plist.ShouldBe("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<dict>\n    <key>Label</key>\n    <string>dev.astar.cloudsync</string>\n    <key>ProgramArguments</key>\n    <array>\n        <string>/Applications/CloudSync.app/Contents/MacOS/CloudSync</string>\n        <string>--minimized</string>\n    </array>\n    <key>RunAtLoad</key>\n    <true/>\n</dict>\n</plist>\n");
    }

    [Theory]
    [InlineData("/Apps/A&B/CloudSync", "<string>/Apps/A&amp;B/CloudSync</string>")]
    [InlineData("/Apps/<x>/CloudSync", "<string>/Apps/&lt;x&gt;/CloudSync</string>")]
    [InlineData("/Apps/\"q\"/CloudSync", "<string>/Apps/&quot;q&quot;/CloudSync</string>")]
    [InlineData("/Apps/it's/CloudSync", "<string>/Apps/it&apos;s/CloudSync</string>")]
    public void when_the_path_has_xml_characters_then_they_are_escaped(string path, string expectedLine)
    {
        LaunchAgentPlistBuilder.Build(path).ShouldContain(expectedLine);
    }

    [Fact]
    public void when_the_path_has_a_line_break_or_control_character_then_it_cannot_add_an_argument()
    {
        var plist = LaunchAgentPlistBuilder.Build("/Apps/x\n</string><string>--evil\u0007");

        plist.Split('\n').Count(line => line.Contains("<string>", StringComparison.Ordinal) && !line.Contains("dev.astar", StringComparison.Ordinal)).ShouldBe(2);
        plist.ShouldNotContain("--evil</string><string>");
    }
}
