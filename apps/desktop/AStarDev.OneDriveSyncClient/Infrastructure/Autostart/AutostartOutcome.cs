namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Base type for the outcomes of registering the application to launch at OS startup.</summary>
public abstract record AutostartOutcome
{
    /// <summary>The application is registered; <paramref name="Location"/> is the file or registry value written.</summary>
    public sealed record Registered(string Location) : AutostartOutcome;

    /// <summary>The registration was removed; <paramref name="Location"/> is the file or registry value removed.</summary>
    public sealed record Unregistered(string Location) : AutostartOutcome;

    /// <summary>There was no registration to remove.</summary>
    public sealed record NotRegistered : AutostartOutcome;

    /// <summary>The application is not running as an installed copy, so nothing was registered.</summary>
    public sealed record NotInstalled : AutostartOutcome;

    /// <summary>Registration was attempted and failed.</summary>
    public sealed record Failed(string Reason) : AutostartOutcome;
}
