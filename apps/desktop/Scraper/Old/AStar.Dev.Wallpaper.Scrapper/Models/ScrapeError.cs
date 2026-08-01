using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Wallpaper.Scrapper.Models;

/// <summary>Base type for every error that a scrape pipeline operation can fail with.</summary>
/// <param name="Message">A human-readable description of the failure.</param>
public abstract record ScrapeError(string Message);

/// <summary>The requested page could not be loaded.</summary>
/// <param name="Url">The URL that failed to load.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record PageLoadFailed(string Url, string Message) : ScrapeError(Message);

/// <summary>The page header text could not be parsed into a <see cref="PageInfo" />.</summary>
/// <param name="HeaderText">The header text that failed to parse, when available.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record PageParseFailed(string? HeaderText, string Message) : ScrapeError(Message);

/// <summary>The image could not be downloaded.</summary>
/// <param name="ImageUrl">The URL of the image that failed to download.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ImageDownloadFailed(string ImageUrl, string Message) : ScrapeError(Message);

/// <summary>The downloaded image could not be saved to disk.</summary>
/// <param name="Path">The path the image was being saved to.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ImageSaveFailed(string Path, string Message) : ScrapeError(Message);

/// <summary>The updated scrape configuration could not be saved.</summary>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ConfigurationSaveFailed(string Message) : ScrapeError(Message);

/// <summary>The file could not be classified.</summary>
/// <param name="FileName">The name of the file that failed to classify.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ClassificationFailed(string FileName, string Message) : ScrapeError(Message);

/// <summary>An unanticipated exception was raised while running the scrape pipeline.</summary>
/// <param name="Exception">The exception that was raised.</param>
public sealed record UnexpectedError(Exception Exception) : ScrapeError(Exception.Message);

/// <summary>The dimensions of a downloaded image could not be read.</summary>
/// <param name="Path">The path of the image whose dimensions failed to read.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ImageDimensionReadFailed(string Path, string Message) : ScrapeError(Message);

/// <summary>An export file could not be imported.</summary>
/// <param name="FilePath">The path of the file that failed to import.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ImportFailed(string FilePath, string Message) : ScrapeError(Message);

/// <summary>An imported payload failed validation.</summary>
/// <param name="Errors">The accumulated validation errors.</param>
/// <param name="Message">A human-readable description of the failure.</param>
public sealed record ValidationFailed(IReadOnlyList<ValidationError> Errors, string Message) : ScrapeError(Message);
