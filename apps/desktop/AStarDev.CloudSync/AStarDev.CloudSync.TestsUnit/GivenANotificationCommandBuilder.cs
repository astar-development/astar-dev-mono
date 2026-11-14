using AStarDev.CloudSync.Notifications;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenANotificationCommandBuilder
{
    [Fact]
    public void when_a_notification_is_built_then_the_title_and_body_are_separate_arguments_after_an_end_of_options_marker()
    {
        var arguments = NotificationCommandBuilder.Arguments(DesktopNotificationFactory.Create("AStar Dev CloudSync", "Photos: 3 files synced."));

        arguments.ShouldBe(["--app-name=CloudSync", "--", "AStar Dev CloudSync", "Photos: 3 files synced."]);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("$(touch /tmp/pwned)")]
    [InlineData("a; rm -rf ~")]
    [InlineData("`id` && echo \"x\" | cat")]
    public void when_the_text_looks_like_an_option_or_shell_syntax_then_it_stays_one_inert_argument(string body)
    {
        var arguments = NotificationCommandBuilder.Arguments(DesktopNotificationFactory.Create("Title", body));

        (arguments.Count, arguments[1], arguments[^1]).ShouldBe((4, "--", body));
    }

    [Fact]
    public void when_the_text_is_empty_then_the_arguments_are_still_complete()
    {
        NotificationCommandBuilder.Arguments(DesktopNotificationFactory.Create(null, null)).ShouldBe(["--app-name=CloudSync", "--", string.Empty, string.Empty]);
    }
}
