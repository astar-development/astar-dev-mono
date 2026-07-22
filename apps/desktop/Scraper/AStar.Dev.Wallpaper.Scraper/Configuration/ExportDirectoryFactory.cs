using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scraper.Configuration;

/// <summary>Factory for constructing validated <see cref="ExportDirectory"/> instances.</summary>
public static class ExportDirectoryFactory
{
    private const string InvalidPathError = "Export directory must not be null, empty, or whitespace.";

    /// <summary>
    /// Creates an <see cref="ExportDirectory"/> from the given raw path string.
    /// Returns a failure result when <paramref name="rawPath"/> is null, empty, or whitespace-only.
    /// </summary>
    public static Result<ExportDirectory, string> Create(string? rawPath)
    {
        if(string.IsNullOrWhiteSpace(rawPath))
            return Result.Failure<ExportDirectory, string>(InvalidPathError);

        return Result.Success<ExportDirectory, string>(ExportDirectory.Restore(rawPath));
    }
}
