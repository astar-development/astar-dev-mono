using System.IO.Abstractions;
using System.Security;
using AStar.Dev.Velopack.Publishing;
using AStarDev.OneDriveSyncClient.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Registers the installed application to launch at OS startup.</summary>
public sealed class AutostartService(IFileSystem fileSystem, IAutostartContextProvider contextProvider, IVelopackUpdateService velopackUpdateService, IWindowsRunKey windowsRunKey, ILogger<AutostartService> logger) : IAutostartService
{
    private const int StartupDelaySeconds = 30;
    private const string LinuxEntryFileName = "astar-onedrive-sync.desktop";
    private const string MacAgentLabel = "com.astar.onedrive-sync";
    private const string WindowsRunValueName = "AStarOneDriveSync";

    /// <inheritdoc />
    public AutostartOutcome EnsureRegistered()
    {
        if (!velopackUpdateService.IsInstalled)
            return AutostartOutcomeFactory.CreateNotInstalled();

        var context = contextProvider.Current();

        try
        {
            return context.Platform switch
            {
                DesktopPlatform.Windows => RegisterWindows(context),
                DesktopPlatform.MacOs => RegisterMacOs(context),
                _ => RegisterLinux(context)
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            return AutostartOutcomeFactory.CreateFailed(ex.Message);
        }
    }

    /// <inheritdoc />
    public AutostartOutcome EnsureUnregistered() => throw new NotImplementedException(logger.ToString());

    /// <inheritdoc />
    public AutostartOutcome Apply(bool launchAtStartup) => throw new NotImplementedException(launchAtStartup.ToString());

    private AutostartOutcome RegisterWindows(AutostartContext context)
    {
        windowsRunKey.SetValue(WindowsRunValueName, AutostartEntryBuilder.BuildWindowsCommand(context.ExecutablePath));

        return AutostartOutcomeFactory.CreateRegistered(WindowsRunValueName);
    }

    private AutostartOutcome RegisterMacOs(AutostartContext context)
    {
        if (context.HomeDirectory.Length == 0)
            return AutostartOutcomeFactory.CreateFailed("The home directory is unknown.");

        string directory = fileSystem.Path.Combine(context.HomeDirectory, "Library", "LaunchAgents");

        return WriteFile(directory, $"{MacAgentLabel}.plist", AutostartEntryBuilder.BuildLaunchAgent(MacAgentLabel, context.ExecutablePath));
    }

    private AutostartOutcome RegisterLinux(AutostartContext context)
    {
        if (context.XdgConfigHome.Length == 0 && context.HomeDirectory.Length == 0)
            return AutostartOutcomeFactory.CreateFailed("The home directory is unknown.");

        string configHome = context.XdgConfigHome.Length > 0 ? context.XdgConfigHome : fileSystem.Path.Combine(context.HomeDirectory, ".config");
        string directory = fileSystem.Path.Combine(configHome, "autostart");

        return WriteFile(directory, LinuxEntryFileName, AutostartEntryBuilder.BuildDesktopEntry(context.ExecutablePath, StartupDelaySeconds));
    }

    private AutostartOutcome WriteFile(string directory, string fileName, string content)
    {
        string path = fileSystem.Path.Combine(directory, fileName);
        _ = fileSystem.Directory.CreateDirectory(directory);
        fileSystem.File.WriteAllText(path, content);

        return AutostartOutcomeFactory.CreateRegistered(path);
    }
}
