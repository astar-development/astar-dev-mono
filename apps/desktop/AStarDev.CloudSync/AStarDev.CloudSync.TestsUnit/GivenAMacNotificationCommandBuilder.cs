using AStarDev.CloudSync.Notifications;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenAMacNotificationCommandBuilder
{
    [Fact]
    public void when_a_notification_is_built_then_the_title_and_body_are_the_last_two_arguments_after_an_end_of_options_marker()
    {
        var command = MacNotificationCommandBuilder.Build("/usr/bin/osascript", DesktopNotificationFactory.Create("AStar Dev CloudSync", "Photos: 3 files synced."));

        (command.Program, string.Join("|", command.Arguments.TakeLast(3))).ShouldBe(("/usr/bin/osascript", "--|AStar Dev CloudSync|Photos: 3 files synced."));
    }

    [Theory]
    [InlineData("\" & do shell script \"id\" & \"")]
    [InlineData("-e display dialog")]
    [InlineData("$(touch /tmp/pwned)")]
    public void when_the_text_looks_like_script_then_it_is_never_part_of_the_script_arguments(string body)
    {
        var command = MacNotificationCommandBuilder.Build("/usr/bin/osascript", DesktopNotificationFactory.Create("Title", body));

        (command.Arguments.Count(argument => argument.Contains(body, StringComparison.Ordinal)), command.Arguments[^1]).ShouldBe((1, body));
    }

    [Fact]
    public void when_a_notification_is_built_then_the_script_reads_the_text_from_its_arguments()
    {
        var command = MacNotificationCommandBuilder.Build("/usr/bin/osascript", DesktopNotificationFactory.Create("T", "B"));

        command.Arguments.Where(argument => argument != "-e").Take(3).ToArray().ShouldBe(["on run argv", "display notification (item 2 of argv) with title (item 1 of argv)", "end run"]);
    }

    [Fact]
    public void when_the_text_is_missing_then_empty_arguments_are_still_passed()
    {
        var command = MacNotificationCommandBuilder.Build("/usr/bin/osascript", DesktopNotificationFactory.Create(null, null));

        command.Arguments.TakeLast(2).ShouldBe([string.Empty, string.Empty]);
    }

    [Fact]
    public void when_osascript_is_at_its_fixed_location_then_it_is_found()
    {
        MacNotificationCommandBuilder.FindProgram(path => path == "/usr/bin/osascript").ShouldBe(Option.Some("/usr/bin/osascript"));
    }

    [Fact]
    public void when_osascript_is_not_at_its_fixed_location_then_nothing_is_found_so_path_is_never_searched()
    {
        MacNotificationCommandBuilder.FindProgram(_ => false).ShouldBe(Option.None<string>());
    }
}
