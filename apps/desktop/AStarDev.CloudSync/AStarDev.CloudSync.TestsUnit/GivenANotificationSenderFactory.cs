using System.Runtime.InteropServices;
using AStarDev.CloudSync.Notifications;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenANotificationSenderFactory
{
    [Fact]
    public void when_the_platform_is_linux_then_the_linux_sender_is_used()
    {
        NotificationSenderFactory.Create(platform => platform == OSPlatform.Linux).GetType().Name.ShouldBe("LinuxNotificationSender");
    }

    [Fact]
    public void when_the_platform_is_windows_then_the_windows_sender_is_used()
    {
        NotificationSenderFactory.Create(platform => platform == OSPlatform.Windows).GetType().Name.ShouldBe("WindowsNotificationSender");
    }

    [Fact]
    public void when_the_platform_is_macos_then_the_mac_sender_is_used()
    {
        NotificationSenderFactory.Create(platform => platform == OSPlatform.OSX).GetType().Name.ShouldBe("MacNotificationSender");
    }

    [Fact]
    public void when_the_platform_is_none_of_these_then_the_sender_says_it_is_not_supported()
    {
        NotificationSenderFactory.Create(_ => false).GetType().Name.ShouldBe("UnsupportedNotificationSender");
    }
}
