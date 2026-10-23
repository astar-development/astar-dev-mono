using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Creates <see cref="ClassificationText"/> instances.</summary>
public static class ClassificationTextFactory
{
    /// <summary>Creates the localised text holder backed by <paramref name="loc"/>.</summary>
    public static ClassificationText Create(ILocalizationService loc) => new(loc);
}
