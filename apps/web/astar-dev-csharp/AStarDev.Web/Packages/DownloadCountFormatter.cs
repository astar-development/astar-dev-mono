using System.Globalization;

namespace AStarDev.Web.Packages;

/// <summary>Formats a NuGet total-download count the same way the previous Astro site's <c>formatDownloads</c> did.</summary>
public static class DownloadCountFormatter
{
    public static string Format(long totalDownloads) =>
        totalDownloads switch
        {
            >= 1_000_000 => $"{totalDownloads / 1_000_000.0:0.0}M",
            >= 1_000 => $"{totalDownloads / 1_000.0:0.0}K",
            _ => totalDownloads.ToString("N0", CultureInfo.GetCultureInfo("en-GB")),
        };
}
