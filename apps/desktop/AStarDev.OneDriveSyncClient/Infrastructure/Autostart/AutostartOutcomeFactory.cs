namespace AStarDev.OneDriveSyncClient.Infrastructure.Autostart;

/// <summary>Creates <see cref="AutostartOutcome"/> instances for each outcome case.</summary>
public static class AutostartOutcomeFactory
{
    /// <summary>Creates the outcome for a successful registration.</summary>
    public static AutostartOutcome CreateRegistered(string location) => new AutostartOutcome.Registered(location);

    /// <summary>Creates the outcome for a registration that was removed.</summary>
    public static AutostartOutcome CreateUnregistered(string location) => new AutostartOutcome.Unregistered(location);

    /// <summary>Creates the outcome for a removal where no registration existed.</summary>
    public static AutostartOutcome CreateNotRegistered() => new AutostartOutcome.NotRegistered();

    /// <summary>Creates the outcome for an application that is not installed.</summary>
    public static AutostartOutcome CreateNotInstalled() => new AutostartOutcome.NotInstalled();

    /// <summary>Creates the outcome for a failed registration.</summary>
    public static AutostartOutcome CreateFailed(string reason) => new AutostartOutcome.Failed(reason);
}
