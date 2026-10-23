using System.IO.Abstractions;
using AStar.Dev.Velopack.Publishing;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Registers the installed application to launch at OS startup.</summary>
public sealed class AutostartService(IFileSystem fileSystem, IAutostartContextProvider contextProvider, IVelopackUpdateService velopackUpdateService, IWindowsRunKey windowsRunKey)
{
    /// <summary>Registers the application for the current platform. Never throws.</summary>
    public AutostartOutcome EnsureRegistered() => throw new NotImplementedException(string.Join(',', fileSystem, contextProvider, velopackUpdateService, windowsRunKey));
}
