using System.IO.Abstractions;
using System.Security;
using AStar.Dev.Velopack.Publishing;
using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Infrastructure.Logging;
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
    private const string HomeDirectoryUnknown = "The home directory is unknown.";

    /// <inheritdoc />
    public AutostartOutcome EnsureRegistered()
    {
        if (!velopackUpdateService.IsInstalled)
            return AutostartOutcomeFactory.CreateNotInstalled();

        return Run(context => context.Platform switch
        {
            DesktopPlatform.Windows => RegisterWindows(context),
            DesktopPlatform.MacOs => MacAgentPath(context).Match(path => WriteFile(path, AutostartEntryBuilder.BuildLaunchAgent(MacAgentLabel, context.ExecutablePath)), FailHomeUnknown),
            _ => LinuxEntryPath(context).Match(path => WriteFile(path, AutostartEntryBuilder.BuildDesktopEntry(context.ExecutablePath, StartupDelaySeconds)), FailHomeUnknown)
        });
    }

    /// <inheritdoc />
    public AutostartOutcome EnsureUnregistered() => Run(context => context.Platform switch
    {
        DesktopPlatform.Windows => UnregisterWindows(),
        DesktopPlatform.MacOs => MacAgentPath(context).Match(DeleteFile, FailHomeUnknown),
        _ => LinuxEntryPath(context).Match(DeleteFile, FailHomeUnknown)
    });

    /// <inheritdoc />
    public AutostartOutcome Apply(bool launchAtStartup)
    {
        var outcome = launchAtStartup ? EnsureRegistered() : EnsureUnregistered();
        LogOutcome(outcome);

        return outcome;
    }

    private AutostartOutcome Run(Func<AutostartContext, AutostartOutcome> action)
    {
        try
        {
            return action(contextProvider.Current());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            return AutostartOutcomeFactory.CreateFailed(ex.Message);
        }
    }

    private AutostartOutcome RegisterWindows(AutostartContext context)
    {
        windowsRunKey.SetValue(WindowsRunValueName, AutostartEntryBuilder.BuildWindowsCommand(context.ExecutablePath));

        return AutostartOutcomeFactory.CreateRegistered(WindowsRunValueName);
    }

    private AutostartOutcome UnregisterWindows()
    {
        windowsRunKey.DeleteValue(WindowsRunValueName);

        return AutostartOutcomeFactory.CreateUnregistered(WindowsRunValueName);
    }

    private Option<string> MacAgentPath(AutostartContext context) => context.HomeDirectory.Length == 0
        ? Option.None<string>()
        : Option.Some(fileSystem.Path.Combine(context.HomeDirectory, "Library", "LaunchAgents", $"{MacAgentLabel}.plist"));

    private Option<string> LinuxEntryPath(AutostartContext context) => (context.XdgConfigHome.Length, context.HomeDirectory.Length) switch
    {
        (0, 0) => Option.None<string>(),
        (0, _) => Option.Some(fileSystem.Path.Combine(context.HomeDirectory, ".config", "autostart", LinuxEntryFileName)),
        _ => Option.Some(fileSystem.Path.Combine(context.XdgConfigHome, "autostart", LinuxEntryFileName))
    };

    private static AutostartOutcome FailHomeUnknown() => AutostartOutcomeFactory.CreateFailed(HomeDirectoryUnknown);

    private AutostartOutcome WriteFile(string path, string content)
    {
        _ = fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        fileSystem.File.WriteAllText(path, content);

        return AutostartOutcomeFactory.CreateRegistered(path);
    }

    private AutostartOutcome DeleteFile(string path)
    {
        if (!fileSystem.File.Exists(path))
            return AutostartOutcomeFactory.CreateNotRegistered();

        fileSystem.File.Delete(path);

        return AutostartOutcomeFactory.CreateUnregistered(path);
    }

    private void LogOutcome(AutostartOutcome outcome)
    {
        switch (outcome)
        {
            case AutostartOutcome.Registered registered:
                OneDriveSyncClientMessages.AutostartRegistered(logger, registered.Location);
                break;
            case AutostartOutcome.Unregistered unregistered:
                OneDriveSyncClientMessages.AutostartUnregistered(logger, unregistered.Location);
                break;
            case AutostartOutcome.Failed failed:
                OneDriveSyncClientMessages.AutostartFailed(logger, failed.Reason);
                break;
        }
    }
}
