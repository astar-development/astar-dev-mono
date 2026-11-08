
namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>Creates <see cref="MainLayout"/> cases.</summary>
public static class MainLayoutFactory
{
    /// <summary>Creates layout A, the unified account list.</summary>
    public static MainLayout CreateUnifiedAccounts() => new MainLayout.UnifiedAccounts();

    /// <summary>Creates layout B, grouped by provider.</summary>
    public static MainLayout CreateGroupedByProvider() => new MainLayout.GroupedByProvider();

    /// <summary>Creates layout C, sync pairs first.</summary>
    public static MainLayout CreateSyncPairsFirst() => new MainLayout.SyncPairsFirst();

    /// <summary>Creates the layout a persisted key names; an unknown or missing key gives the default, layout A.</summary>
    /// <param name="key">The persisted key, as produced by <see cref="MainLayoutExtensions.ToKey"/>.</param>
    public static MainLayout FromKey(string? key) => key?.Trim().ToUpperInvariant() switch
    {
        "B" => CreateGroupedByProvider(),
        "C" => CreateSyncPairsFirst(),
        _ => CreateUnifiedAccounts()
    };
}
