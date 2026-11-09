using System.Diagnostics;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;

namespace AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;

public sealed class RecordingProcessLauncher : IProcessLauncher
{
    public List<ProcessStartInfo> Started { get; } = [];

    public void Start(ProcessStartInfo startInfo) => Started.Add(startInfo);

    public static string ExpectedPlatformOpener()
        => OperatingSystem.IsWindows() ? "explorer" : ExpectedNonWindowsOpener();

    private static string ExpectedNonWindowsOpener() => OperatingSystem.IsMacOS() ? "open" : "xdg-open";
}
