using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Notifications;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Notifications;

public sealed class GivenANotificationCommandBuilder
{
    [Fact]
    public void when_platform_is_linux_then_notify_send_is_used_with_arguments_after_the_option_terminator()
    {
        var notification = DesktopNotificationFactory.Create("Sync complete", "Alice");

        var command = NotificationCommandBuilder.Build(notification, DesktopPlatformFactory.CreateLinux());

        command.ShouldBe(Option.Some(ProcessCommandFactory.Create("notify-send", "--app-name=AStar OneDrive Sync", "--", "Sync complete", "Alice")));
    }

    [Fact]
    public void when_linux_title_starts_with_a_dash_then_it_is_still_passed_after_the_option_terminator()
    {
        var notification = DesktopNotificationFactory.Create("--urgency=critical", "body");

        var command = NotificationCommandBuilder.Build(notification, DesktopPlatformFactory.CreateLinux());

        command.Match(c => c.Arguments.ToList().IndexOf("--") < c.Arguments.ToList().IndexOf("--urgency=critical"), () => false).ShouldBeTrue();
    }

    [Fact]
    public void when_platform_is_macos_then_osascript_display_notification_is_used()
    {
        var notification = DesktopNotificationFactory.Create("Sync complete", "Alice");

        var command = NotificationCommandBuilder.Build(notification, DesktopPlatformFactory.CreateMacOs());

        command.ShouldBe(Option.Some(ProcessCommandFactory.Create("osascript", "-e", "display notification \"Alice\" with title \"Sync complete\"")));
    }

    [Fact]
    public void when_macos_text_contains_quotes_and_backslashes_then_they_are_escaped()
    {
        var notification = DesktopNotificationFactory.Create("T\"itle", "a\\b\" & do shell script \"x");

        var command = NotificationCommandBuilder.Build(notification, DesktopPlatformFactory.CreateMacOs());

        command.ShouldBe(Option.Some(ProcessCommandFactory.Create("osascript", "-e", "display notification \"a\\\\b\\\" & do shell script \\\"x\" with title \"T\\\"itle\"")));
    }

    [Fact]
    public void when_platform_is_windows_then_no_command_is_returned()
    {
        var command = NotificationCommandBuilder.Build(DesktopNotificationFactory.Create("t", "b"), DesktopPlatformFactory.CreateWindows());

        command.ShouldBe(Option.None<ProcessCommand>());
    }
}
