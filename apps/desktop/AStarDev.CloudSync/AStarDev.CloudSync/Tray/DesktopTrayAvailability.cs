using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.ViewModels.Tray;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Tray;

/// <summary>Windows and macOS always have a tray. On Linux the tray needs a StatusNotifier watcher on the session bus, which GNOME lacks without an extension; the bus is asked through <c>gdbus</c>. A <c>gdbus</c> missing from its fixed locations (never searched on <c>PATH</c>), a timeout or any other doubt counts as no tray, so the window is minimised rather than hidden (#1409).</summary>
[ExcludeFromCodeCoverage(Justification = "Asks the desktop session bus through an external process; the close behaviour that uses the answer is tested.")]
internal sealed class DesktopTrayAvailability : ITrayAvailability
{
    private const int TimeoutMilliseconds = 2000;
    private static readonly string[] ProgramLocations = ["/usr/bin/gdbus", "/usr/local/bin/gdbus", "/bin/gdbus"];

    private const string WatcherName = "org.kde.StatusNotifierWatcher";

    /// <inheritdoc />
    public TrayAvailability Check() => OperatingSystem.IsLinux() && !WatcherIsRunning()
        ? TrayAvailabilityFactory.CreateMissing()
        : TrayAvailabilityFactory.CreateAvailable();

    private static bool WatcherIsRunning() => Try.Run(AskTheBus).Match(running => running, _ => false);

    private static bool AskTheBus() => ProgramLocations.FirstOrNone(File.Exists).Match(AskTheBusWith, () => false);

    private static bool AskTheBusWith(string program)
    {
        var start = new ProcessStartInfo(program, ["call", "--session", "--dest", "org.freedesktop.DBus", "--object-path", "/org/freedesktop/DBus", "--method", "org.freedesktop.DBus.NameHasOwner", WatcherName]) { RedirectStandardOutput = true, UseShellExecute = false };
        using var process = Process.Start(start);
        if (process is null || !process.WaitForExit(TimeoutMilliseconds))
            return false;

        return process.StandardOutput.ReadToEnd().Contains("true", StringComparison.Ordinal);
    }
}
