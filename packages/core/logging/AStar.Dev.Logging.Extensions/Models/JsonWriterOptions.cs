namespace AStar.Dev.Logging.Extensions.Models;

/// <summary>
///     Represents options for configuring the behavior of a JSON writer.
/// </summary>
public sealed class JsonWriterOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether the JSON output should be indented.
    ///     If set to <see langword="true" />, the JSON will be formatted with additional
    ///     whitespace for readability (e.g., line breaks and indentation).
    ///     If set to <see langword="false" />, the JSON will be written as a single line
    ///     without extra whitespace, minimizing the output size.
    /// </summary>
    public bool Indented { get; set; }
}