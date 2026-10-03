using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace AStarDev.Utilities;

/// <summary>The <see cref="StringExtensions" /> class contains some useful methods to enable checks to be performed in a more fluid, English sentence, style</summary>
public static class StringExtensions
{
    extension([NotNullWhen(false)] string? value)
    {
        /// <summary>The IsNull method, as you might expect, checks whether the string is, in fact, null</summary>
        /// <returns>True if the string is null, False otherwise</returns>
        /// <remarks>
        ///     Kept as a method rather than a property: the [NotNullWhen] attribute on the receiver
        ///     enables nullable flow analysis at call sites (e.g. <c>if (value.IsNotNull())</c> narrows
        ///     <c>value</c> to non-null afterwards), which requires an invocable member.
        /// </remarks>
        public bool IsNull() =>
            value is null;

        /// <summary>The IsNullOrWhiteSpace method, as you might expect, checks whether the string is, in fact, null, empty or whitespace</summary>
        /// <returns>True if the string is null, empty or whitespace, False otherwise</returns>
        /// <remarks>
        ///     Kept as a method rather than a property: its name is identical to the BCL's own
        ///     <see cref="string.IsNullOrWhiteSpace(string?)" /> static method, and exposing it as a
        ///     property on the same underlying type creates a genuine member-lookup ambiguity at call
        ///     sites (the compiler cannot tell them apart).
        /// </remarks>
        public bool IsNullOrWhiteSpace() =>
            string.IsNullOrWhiteSpace(value);
    }

    extension([NotNullWhen(true)] string? value)
    {
        /// <summary>The IsNotNull method, as you might expect, checks whether the string is not null</summary>
        /// <returns>True if the string is not null, False otherwise</returns>
        /// <remarks>
        ///     Kept as a method rather than a property: the [NotNullWhen] attribute on the receiver
        ///     enables nullable flow analysis at call sites (e.g. <c>if (value.IsNotNull())</c> narrows
        ///     <c>value</c> to non-null afterwards), which requires an invocable member.
        /// </remarks>
        public bool IsNotNull() =>
            !value.IsNull();

        /// <summary>The IsNotNullOrWhiteSpace method, as you might expect, checks whether the string is not null, empty or whitespace</summary>
        /// <returns>True if the string is not null, empty or whitespace, False otherwise</returns>
        public bool IsNotNullOrWhiteSpace() =>
            !value.IsNullOrWhiteSpace();
    }

    extension(string json)
    {
        /// <summary>
        ///     The FromJson method, as you might expect, converts the supplied JSON to the specified object. When no options are supplied, the default <see cref="Constants.WebDeserialisationSettings" /> will be used
        ///     to ensure the deserialisation is performed in a consistent manner across the application
        /// </summary>
        /// <typeparam name="T">The required type of the object to deserialise to</typeparam>
        /// <param name="options">
        ///     Allows the specific <see href="JsonSerializerOptions">options</see> to be set to control
        ///     deserialisation. When null, the default <see href="Constants.WebDeserialisationSettings">WebDeserialisationSettings</see> will be used
        /// </param>
        /// <returns>A deserialised object based on the original JSON</returns>
        public T FromJson<T>(JsonSerializerOptions? options = null) =>
            options is null
                ? JsonSerializer.Deserialize<T>(json, Constants.WebDeserialisationSettings)!
                : JsonSerializer.Deserialize<T>(json, options)!;

        /// <summary>
        ///     The SanitizeFilePath method replaces invalid or undesirable characters in a file path
        ///     with a space character to ensure a clean and sanitized string representation of the path.
        /// </summary>
        /// <returns>A sanitized version of the file path with specified characters replaced by spaces</returns>
        /// <example>
        ///     Example Usage:
        ///     string originalPath = "path/to-some_file.txt";
        ///     string sanitizedPath = originalPath.SanitizeFilePath();
        ///     // sanitizedPath will be: "path to some file.txt"
        /// </example>
        public string SanitizeFilePath() => json.IsNotNullOrWhiteSpace() ? json.Replace(Path.DirectorySeparatorChar, ' ')
                    .Replace(Path.AltDirectorySeparatorChar, ' ')
                    .Replace('-', ' ')
                    .Replace('_', ' ') : string.Empty;
    }

    extension(string fileName)
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool IsImage => fileName.IsNotNullOrWhiteSpace() && (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool IsNumberOnly => fileName.All(c => char.IsDigit(c) || c == '_' || c == '.');
    }

    extension(string value)
    {
        /// <summary>The TruncateIfRequired method will, as the name suggests, truncate the string if the length exceeds the specified length</summary>
        /// <param name="truncateLength">The maximum length the string should be truncated to if required</param>
        /// <returns>The specified string or the truncated version</returns>
        public string TruncateIfRequired(int truncateLength)
        {
            if (string.IsNullOrEmpty(value) || truncateLength <= 0) return value;

            if (value.Length <= truncateLength) return value;

            var length = char.IsHighSurrogate(value[truncateLength - 1]) ? truncateLength - 1 : truncateLength;

            return value[..length];
        }

        /// <summary>The RemoveInvalidFileNameCharacters method removes the characters that are not allowed in file or directory names on any supported platform, along with control characters.</summary>
        /// <returns>The string without the invalid characters, or an empty string when the value is null or empty</returns>
        public string RemoveInvalidFileNameCharacters()
            => string.IsNullOrEmpty(value) ? string.Empty : string.Concat(value.Where(character => !"\\/:*?\"<>|".Contains(character, StringComparison.Ordinal) && !char.IsControl(character)));

        /// <summary>The RemoveTrailing method will, as the name suggests, remove the specified character from the end if it exists</summary>
        /// <param name="removeTrailing">The character to remove from the end if it exists</param>
        /// <returns>The original or updated string</returns>
        public string RemoveTrailing(string removeTrailing)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(removeTrailing)) return value;

            return value.EndsWith(removeTrailing, StringComparison.OrdinalIgnoreCase)
                ? value[..^removeTrailing.Length]
                : value;
        }

        /// <summary>The EnsureTrailing method will, as the name suggests, ensure the string ends with the specified character</summary>
        /// <param name="ensureTrailing">The character to ensure is at the end of the string</param>
        /// <returns>The original or updated string</returns>
        public string EnsureTrailing(string ensureTrailing)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(ensureTrailing)) return value;

            return value.EndsWith(ensureTrailing, StringComparison.OrdinalIgnoreCase)
                ? value
                : value + ensureTrailing;
        }

        /// <summary>The CaseInsensitiveContains method checks if the string contains the specified substring, ignoring case.</summary>
        /// <param name="contains">The substring to search for</param>
        /// <returns>True if the value contains the substring, false otherwise</returns>
        public bool CaseInsensitiveContains(string contains)
            => value.Contains(contains, StringComparison.OrdinalIgnoreCase);

        /// <summary>The CaseInsensitiveEquals method checks if the string equals the specified substring, ignoring case.</summary>
        /// <param name="equals">The string to compare with</param>
        /// <returns>True if the value equals the specified string, false otherwise</returns>
        public bool CaseInsensitiveEquals(string equals)
            => value.Equals(equals, StringComparison.OrdinalIgnoreCase);

        /// <summary>The TitleCased method converts a string to title case using the specified culture.</summary>
        /// <param name="cultureName">The culture to use for title casing</param>
        /// <returns>The title-cased string</returns>
        public string ToTitleCase(string cultureName = "en-GB")
#pragma warning disable CA1308 // Normalize strings to uppercase
            => string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : new System.Globalization.CultureInfo(cultureName, false).TextInfo.ToTitleCase(value.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase

        /// <summary>The ToDirectorySlug method converts a string to a directory-safe slug by lower-casing it and replacing spaces with hyphens.</summary>
        /// <returns>The slugified string</returns>
        public string ToDirectorySlug()
#pragma warning disable CA1308 // Normalize strings to uppercase
            => value.IsNullOrWhiteSpace() ? string.Empty : value.Trim().ToLowerInvariant().Replace(' ', '-');
#pragma warning restore CA1308 // Normalize strings to uppercase
    }

    extension(Uri value)
    {
        /// <summary>The EnsureTrailing method will, as the name suggests, ensure the string ends with the specified character</summary>
        /// <param name="ensureTrailing">The character to ensure is at the end of the string</param>
        /// <returns>The original or updated string</returns>
        public string EnsureTrailing(string ensureTrailing)
        {
            if (value is null || string.IsNullOrEmpty(ensureTrailing)) return string.Empty;

            string valueString = value.ToString();
            return valueString.EndsWith(ensureTrailing, StringComparison.OrdinalIgnoreCase)
                ? valueString
                : valueString + ensureTrailing;
        }

        /// <summary>The EnsureTrailing method will, as the name suggests, ensure the string ends with the specified character</summary>
        /// <returns>The original or updated string</returns>
        public string EnsureTrailingSlash()
            => value.EnsureTrailing("/");
    }

    extension(string path)
    {
        /// <summary>The NormalizeLinux method normalizes a file path to a Linux-style format by replacing backslashes with forward slashes,</summary>
        /// <returns>The normalized file path, prefixed with a forward slash if not already prefixed</returns>
        public string NormalizeLinux()
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            string normalized = path.Trim()
                       .Replace("\\", "/", StringComparison.Ordinal)
                       .TrimEnd('/');

            return normalized.StartsWith('/') ? normalized : "/" + normalized;
        }

        /// <summary>The NormalizeWindows method normalizes a file path to a Windows-style format by replacing forward slashes with backslashes,</summary>
        /// <returns>The normalized file path, prefixed with a backslash if not already prefixed</returns>
        public string NormalizeWindows()
        {
            if (string.IsNullOrWhiteSpace(path))
                return "\\";

            string normalized = path.Trim()
                       .Replace("/", "\\", comparisonType: StringComparison.Ordinal)
                       .TrimEnd('\\');

            return normalized.StartsWith('\\') ? normalized : "\\" + normalized;
        }
    }

    extension(long fileSize)
    {
        /// <summary>The FileSizeText method converts a file size in bytes to a human-readable string format (B, KB, MB).</summary>
        /// <returns>The human-readable string format</returns>
        public string FileSizeToText() => fileSize switch
        {
            // add gigabyte formatting
            0 => string.Empty,
            < 1024 => $"{fileSize} B",
            < 1024 * 1024 => $"{fileSize / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{fileSize / (1024.0 * 1024):F1} MB",
            _ => $"{fileSize / (1024.0 * 1024 * 1024):F1} GB"
        };
    }
}
