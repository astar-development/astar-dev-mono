using System.Diagnostics;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Shell;

/// <summary>Starts operating-system processes, such as the platform file manager or file opener.</summary>
public interface IProcessLauncher
{
    /// <summary>Starts a process described by <paramref name="startInfo"/>.</summary>
    /// <param name="startInfo">The executable, arguments and options for the process.</param>
    void Start(ProcessStartInfo startInfo);
}
