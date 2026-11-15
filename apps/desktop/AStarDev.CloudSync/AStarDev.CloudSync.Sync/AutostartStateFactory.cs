namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="AutostartState"/> and <see cref="AutostartBlocker"/> cases.</summary>
public static class AutostartStateFactory
{
    /// <summary>Creates the <see cref="AutostartState.Enabled"/> case.</summary>
    public static AutostartState CreateEnabled() => new AutostartState.Enabled();

    /// <summary>Creates the <see cref="AutostartState.Disabled"/> case.</summary>
    public static AutostartState CreateDisabled() => new AutostartState.Disabled();

    /// <summary>Creates the <see cref="AutostartState.NotSupported"/> case.</summary>
    /// <param name="blocker">Why the app cannot register itself.</param>
    public static AutostartState CreateNotSupported(AutostartBlocker blocker) => new AutostartState.NotSupported(blocker);

    /// <summary>Creates the <see cref="AutostartBlocker.NotInstalledBuild"/> case.</summary>
    public static AutostartBlocker CreateNotInstalledBuild() => new AutostartBlocker.NotInstalledBuild();

    /// <summary>Creates the <see cref="AutostartBlocker.PlatformNotSupported"/> case.</summary>
    public static AutostartBlocker CreatePlatformNotSupported() => new AutostartBlocker.PlatformNotSupported();
}
