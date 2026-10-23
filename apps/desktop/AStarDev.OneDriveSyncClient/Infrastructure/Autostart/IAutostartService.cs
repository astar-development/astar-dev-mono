namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Registers or removes the application's launch-at-OS-startup registration.</summary>
public interface IAutostartService
{
    /// <summary>Registers the installed application to launch at OS startup. Never throws.</summary>
    AutostartOutcome EnsureRegistered();

    /// <summary>Removes any launch-at-OS-startup registration. Never throws.</summary>
    AutostartOutcome EnsureUnregistered();

    /// <summary>Registers when <paramref name="launchAtStartup"/> is true, otherwise removes the registration. Logs the outcome. Never throws.</summary>
    AutostartOutcome Apply(bool launchAtStartup);
}
