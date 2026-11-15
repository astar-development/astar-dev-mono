namespace AStarDev.CloudSync.ViewModels.Tray;

/// <summary>Creates <see cref="TrayAvailability"/> values.</summary>
public static class TrayAvailabilityFactory
{
    /// <summary>Creates the <see cref="TrayAvailability.Available"/> case.</summary>
    public static TrayAvailability CreateAvailable() => new TrayAvailability.Available();

    /// <summary>Creates the <see cref="TrayAvailability.Missing"/> case.</summary>
    public static TrayAvailability CreateMissing() => new TrayAvailability.Missing();
}
