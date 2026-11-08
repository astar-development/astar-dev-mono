
namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>Extensions for <see cref="MainLayout"/>.</summary>
public static class MainLayoutExtensions
{
    /// <summary>The key used to persist the layout: "A", "B" or "C".</summary>
    /// <param name="layout">The layout to persist.</param>
    /// <returns>The persistence key, read back by <see cref="MainLayoutFactory.FromKey"/>.</returns>
    public static string ToKey(this MainLayout layout) => layout switch
    {
        MainLayout.GroupedByProvider => "B",
        MainLayout.SyncPairsFirst => "C",
        _ => "A"
    };
}
