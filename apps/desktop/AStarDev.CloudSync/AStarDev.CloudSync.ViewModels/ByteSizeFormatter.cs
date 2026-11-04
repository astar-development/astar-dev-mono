using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>Formats byte counts as localised sizes, e.g. "728.9 GB".</summary>
public static class ByteSizeFormatter
{
    private const double Kilobyte = 1024;

    private static readonly string[] unitKeys = ["Size.Kilobytes", "Size.Megabytes", "Size.Gigabytes", "Size.Terabytes"];

    /// <summary>Formats <paramref name="bytes"/> in the largest unit that keeps the number at or above 1.</summary>
    /// <param name="bytes">The byte count. Negative values are treated as zero.</param>
    /// <param name="localization">Supplies the unit text and culture.</param>
    /// <returns>The localised size.</returns>
    public static string Format(long bytes, ILocalizationService localization)
    {
        long safeBytes = Math.Max(bytes, 0);
        if (safeBytes < Kilobyte)
            return localization.GetLocal("Size.Bytes", safeBytes.ToString("N0", localization.CurrentCulture));

        double value = safeBytes / Kilobyte;
        int unitIndex = 0;
        while (value >= Kilobyte && unitIndex < unitKeys.Length - 1)
        {
            value /= Kilobyte;
            unitIndex++;
        }

        return localization.GetLocal(unitKeys[unitIndex], value.ToString("N1", localization.CurrentCulture));
    }
}
