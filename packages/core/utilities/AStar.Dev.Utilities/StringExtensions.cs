using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace AStar.Dev.Utilities;

/// <summary>
///     The <see cref="StringExtensions" /> class contains some useful methods to enable checks to be
///     performed in a more fluid, English sentence, style
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     The IsNull method, as you might expect, checks whether the string is, in fact, null
    /// </summary>
    /// <param name="value">The string to check for being null</param>
    /// <returns>True if the string is null, False otherwise</returns>
    public static bool IsNull([NotNullWhen(false)] this string? value) =>
        value is null;

    /// <summary>
    ///     The IsNotNull method, as you might expect, checks whether the string is not null
    /// </summary>
    /// <param name="value">The string to check for being not null</param>
    /// <returns>True if the string is not null, False otherwise</returns>
    public static bool IsNotNull([NotNullWhen(true)] this string? value) =>
        !value.IsNull();

    /// <summary>
    ///     The IsNullOrWhiteSpace method, as you might expect, checks whether the string is, in fact, null, empty or
    ///     whitespace
    /// </summary>
    /// <param name="value">The string to check for being null, empty or whitespace</param>
    /// <returns>True if the string is null, empty or whitespace, False otherwise</returns>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value) =>
        string.IsNullOrWhiteSpace(value);

    /// <summary>
    ///     The IsNotNullOrWhiteSpace method, as you might expect, checks whether the string is not null, empty or whitespace
    /// </summary>
    /// <param name="value">The string to check for being not null, empty or whitespace</param>
    /// <returns>True if the string is not null, empty or whitespace, False otherwise</returns>
    public static bool IsNotNullOrWhiteSpace([NotNullWhen(true)] this string? value) =>
        !value.IsNullOrWhiteSpace();

    /// <summary>
    ///     The FromJson method, as you might expect, converts the supplied JSON to the specified object
    /// </summary>
    /// <typeparam name="T">The required type of the object to deserialise to</typeparam>
    /// <param name="json">The JSON representation of the object</param>
    /// <returns>A deserialised object based on the original JSON</returns>
    public static T FromJson<T>(this string json) =>
        JsonSerializer.Deserialize<T>(json)!;

    /// <summary>
    ///     The FromJson method, as you might expect, converts the supplied JSON to the specified object
    /// </summary>
    /// <typeparam name="T">The required type of the object to deserialise to</typeparam>
    /// <param name="json">The JSON representation of the object</param>
    /// <param name="options">
    ///     Allows the specific <see href="JsonSerializerOptions">options</see> to be set to control
    ///     deserialisation
    /// </param>
    /// <returns>A deserialised object based on the original JSON</returns>
    public static T FromJson<T>(this string json, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<T>(json, options)!;

    /// <summary>
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static bool IsImage(this string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;

        return fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static bool IsNumberOnly(this string fileName) =>
        fileName.All(c => char.IsDigit(c) || c == '_' || c == '.');

    /// <summary>
    ///     The TruncateIfRequired method will, as the name suggests, truncate the string if the length exceeds the specified length
    /// </summary>
    /// <param name="value">The raw string to potentially truncate</param>
    /// <param name="truncateLength">The maximum length the string should be truncated to if required</param>
    /// <returns>The specified string or the truncated version</returns>
    public static string TruncateIfRequired(this string value, int truncateLength)
    {
        if (string.IsNullOrEmpty(value) || truncateLength <= 0) return value;

        return value.Length > truncateLength ? value[..truncateLength] : value;
    }

    /// <summary>
    ///     The RemoveTrailing method will, as the name suggests, remove the specified character from the end if it exists
    /// </summary>
    /// <param name="value">The raw string to potentially remove the trailing character from</param>
    /// <param name="removeTrailing">The character to remove from the end if it exists</param>
    /// <returns>The original or updated string</returns>
    public static string RemoveTrailing(this string value, string removeTrailing)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(removeTrailing)) return value;

        return value.EndsWith(removeTrailing, StringComparison.OrdinalIgnoreCase)
            ? value[..^removeTrailing.Length]
            : value;
    }

    /// <summary>
    ///     The EnsureTrailing method will, as the name suggests, ensure the string ends with the specified character
    /// </summary>
    /// <param name="value">The raw string to potentially add the trailing character to</param>
    /// <param name="ensureTrailing">The character to ensure is at the end of the string</param>
    /// <returns>The original or updated string</returns>
    public static string EnsureTrailing(this string value, string ensureTrailing)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(ensureTrailing)) return value;

        return value.EndsWith(ensureTrailing, StringComparison.OrdinalIgnoreCase)
            ? value
            : value + ensureTrailing;
    }

    /// <summary>
    ///     The EnsureTrailing method will, as the name suggests, ensure the string ends with the specified character
    /// </summary>
    /// <param name="value">The raw string to potentially add the trailing character to</param>
    /// <param name="ensureTrailing">The character to ensure is at the end of the string</param>
    /// <returns>The original or updated string</returns>
    public static string EnsureTrailing(this Uri value, string ensureTrailing)
    {
        if (value is null || string.IsNullOrEmpty(ensureTrailing)) return string.Empty;

        string valueString = value.ToString();
        return valueString.EndsWith(ensureTrailing, StringComparison.OrdinalIgnoreCase)
            ? valueString
            : valueString + ensureTrailing;
    }

    /// <summary>
    ///     The EnsureTrailing method will, as the name suggests, ensure the string ends with the specified character
    /// </summary>
    /// <param name="value">The raw string to potentially add the trailing character to</param>
    /// <returns>The original or updated string</returns>
    public static string EnsureTrailingSlash(this Uri value)
        => value.EnsureTrailing("/");

    /// <summary>
    ///     The SanitizeFilePath method replaces invalid or undesirable characters in a file path
    ///     with a space character to ensure a clean and sanitized string representation of the path.
    /// </summary>
    /// <param name="json">The JSON representation of the object</param>
    /// <returns>A sanitized version of the file path with specified characters replaced by spaces</returns>
    /// <example>
    ///     Example Usage:
    ///     string originalPath = "path/to-some_file.txt";
    ///     string sanitizedPath = originalPath.SanitizeFilePath();
    ///     // sanitizedPath will be: "path to some file.txt"
    /// </example>
    public static string SanitizeFilePath(this string json) => json.IsNotNullOrWhiteSpace() ? json.Replace(Path.DirectorySeparatorChar, ' ')
                .Replace(Path.AltDirectorySeparatorChar, ' ')
                .Replace('-', ' ')
                .Replace('_', ' ') : string.Empty;

    /// <summary>
    ///    The NormalizeLinux method normalizes a file path to a Linux-style format by replacing backslashes with forward slashes,
    /// </summary>
    /// <param name="path">The file path to normalize</param>
    /// <returns>The normalized file path, prefixed with a forward slash if not already prefixed</returns>
    public static string NormalizeLinux(this string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        path = path.Trim()
                   .Replace("\\", "/", StringComparison.Ordinal)
                   .TrimEnd('/');

        return path.StartsWith('/') ? path : "/" + path;
    }

    /// <summary>
    ///   The NormalizeWindows method normalizes a file path to a Windows-style format by replacing forward slashes with backslashes,
    /// </summary>
    /// <param name="path">The file path to normalize</param>
    /// <returns>The normalized file path, prefixed with a backslash if not already prefixed</returns>
    public static string NormalizeWindows(this string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "\\";

        path = path.Trim()
                   .Replace("/", "\\", comparisonType: StringComparison.Ordinal)
                   .TrimEnd('\\');

        return path.StartsWith('\\') ? path : "\\" + path;
    }

    /// <summary>
    ///    The FileSizeText method converts a file size in bytes to a human-readable string format (B, KB, MB).
    /// </summary>
    /// <param name="fileSize">The file size in bytes (represented as a long integer)</param>
    /// <returns>The human-readable string format</returns>
    public static string FileSizeToText(this long fileSize) => fileSize switch
    {
        // add gigabyte formatting
        0 => string.Empty,
        < 1024 => $"{fileSize} B",
        < 1024 * 1024 => $"{fileSize / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{fileSize / (1024.0 * 1024):F1} MB",
        _ => $"{fileSize / (1024.0 * 1024 * 1024):F1} GB"
    };

    /// <summary>
    ///     The CaseInsensitiveContains method checks if the string contains the specified substring, ignoring case.
    /// </summary>
    /// <param name="value">The string to search within</param>
    /// <param name="contains">The substring to search for</param>
    /// <returns>True if the value contains the substring, false otherwise</returns>
    public static bool CaseInsensitiveContains(this string value, string contains)
        => value.Contains(contains, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     The CaseInsensitiveEquals method checks if the string equals the specified substring, ignoring case.
    /// </summary>
    /// <param name="value">The string to compare</param>
    /// <param name="equals">The string to compare with</param>
    /// <returns>True if the value equals the specified string, false otherwise</returns>
    public static bool CaseInsensitiveEquals(this string value, string equals)
        => value.Equals(equals, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///   The TitleCased method converts a string to title case using the specified culture.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <param name="cultureName">The culture to use for title casing</param>
    /// <returns>The title-cased string</returns>
    public static string ToTitleCase(this string value, string cultureName = "en-GB")
#pragma warning disable CA1308 // Normalize strings to uppercase
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new System.Globalization.CultureInfo(cultureName, false).TextInfo.ToTitleCase(value.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
}
