namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>
/// Immutable value object representing a validated Import/Export directory.
/// Construct via <see cref="ExportDirectoryFactory.Create"/> — never use the public constructor directly.
/// </summary>
public sealed record ExportDirectory
{
    private ExportDirectory(string value) => Value = value;

    /// <summary>The underlying directory path.</summary>
    public string Value { get; }

    /// <summary>
    /// Restores an <see cref="ExportDirectory"/> from a previously validated value.
    /// Bypasses validation — for <see cref="ExportDirectoryFactory.Create"/> use only.
    /// </summary>
    internal static ExportDirectory Restore(string value) => new(value);
}
