using System.Runtime.InteropServices;
using AStarDev.CloudSync.Sync;

namespace AStarDev.CloudSync.Notifications;

/// <summary>Picks the notification sender for the operating system (#1410).</summary>
public static class NotificationSenderFactory
{
    /// <summary>Creates the sender for the platform, or one that says it is not supported.</summary>
    /// <param name="isPlatform">Tells whether the app runs on a platform.</param>
    public static INotificationSender Create(Func<OSPlatform, bool> isPlatform)
    {
        if (isPlatform(OSPlatform.Linux))
            return new LinuxNotificationSender();

        if (isPlatform(OSPlatform.Windows))
            return new WindowsNotificationSender();

        return isPlatform(OSPlatform.OSX) ? new MacNotificationSender() : new UnsupportedNotificationSender();
    }
}
