using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Guard.Clauses;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>Factory methods for creating instances of the <see cref="ScrapeError" /> discriminated union.</summary>
public static class ScrapeErrorFactory
{
    /// <summary>Creates a <see cref="PageLoadFailed" /> error.</summary>
    public static PageLoadFailed CreatePageLoadFailed(string url, string message)
    {
        GuardAgainst.Null(url);
        GuardAgainst.Null(message);

        return new(url, message);
    }

    /// <summary>Creates a <see cref="PageParseFailed" /> error.</summary>
    public static PageParseFailed CreatePageParseFailed(string? headerText, string message)
    {
        GuardAgainst.Null(message);

        return new(headerText, message);
    }

    /// <summary>Creates an <see cref="ImageDownloadFailed" /> error.</summary>
    public static ImageDownloadFailed CreateImageDownloadFailed(string imageUrl, string message)
    {
        GuardAgainst.Null(imageUrl);
        GuardAgainst.Null(message);

        return new(imageUrl, message);
    }

    /// <summary>Creates an <see cref="ImageSaveFailed" /> error.</summary>
    public static ImageSaveFailed CreateImageSaveFailed(string path, string message)
    {
        GuardAgainst.Null(path);
        GuardAgainst.Null(message);

        return new(path, message);
    }

    /// <summary>Creates a <see cref="ConfigurationSaveFailed" /> error.</summary>
    public static ConfigurationSaveFailed CreateConfigurationSaveFailed(string message)
    {
        GuardAgainst.Null(message);

        return new(message);
    }

    /// <summary>Creates a <see cref="ClassificationFailed" /> error.</summary>
    public static ClassificationFailed CreateClassificationFailed(string fileName, string message)
    {
        GuardAgainst.Null(fileName);
        GuardAgainst.Null(message);

        return new(fileName, message);
    }

    /// <summary>Creates an <see cref="UnexpectedError" /> error.</summary>
    public static UnexpectedError CreateUnexpectedError(Exception exception)
    {
        GuardAgainst.Null(exception);

        return new(exception);
    }

    /// <summary>Creates an <see cref="ImageDimensionReadFailed" /> error.</summary>
    public static ImageDimensionReadFailed CreateImageDimensionReadFailed(string path, string message)
    {
        GuardAgainst.Null(path);
        GuardAgainst.Null(message);

        return new(path, message);
    }

    /// <summary>Creates an <see cref="ImportFailed" /> error.</summary>
    public static ImportFailed CreateImportFailed(string filePath, string message)
    {
        GuardAgainst.Null(filePath);
        GuardAgainst.Null(message);

        return new(filePath, message);
    }

    /// <summary>Creates a <see cref="ValidationFailed" /> error.</summary>
    public static ValidationFailed CreateValidationFailed(IReadOnlyList<ValidationError> errors, string message)
    {
        GuardAgainst.Null(errors);
        GuardAgainst.Null(message);

        return new(errors, message);
    }
}
