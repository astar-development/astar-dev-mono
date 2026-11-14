namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="NetworkAvailability"/> cases.</summary>
public static class NetworkAvailabilityFactory
{
    /// <summary>Creates the <see cref="NetworkAvailability.Available"/> case.</summary>
    public static NetworkAvailability CreateAvailable() => new NetworkAvailability.Available();

    /// <summary>Creates the <see cref="NetworkAvailability.Unavailable"/> case.</summary>
    public static NetworkAvailability CreateUnavailable() => new NetworkAvailability.Unavailable();
}
