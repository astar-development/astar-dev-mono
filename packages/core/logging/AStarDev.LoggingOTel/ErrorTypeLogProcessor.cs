using OpenTelemetry;
using OpenTelemetry.Logs;

namespace AStarDev.LoggingOTel;

/// <summary>
///     Copies the PascalCase <c>ErrorType</c> or <c>ExceptionType</c> attribute of a failed operation to the semantic-convention key
///     <c>error.type</c>, which a <c>[LoggerMessage]</c> placeholder cannot produce because it cannot contain a dot. Records without
///     either attribute, such as successes, are left alone. When a record carries both, the first one in template order wins. It must be added
///     before any exporter.
/// </summary>
public sealed class ErrorTypeLogProcessor : BaseProcessor<LogRecord>
{
    /// <summary>The semantic-convention attribute key.</summary>
    public const string AttributeKey = "error.type";

    /// <summary>The semantic-convention fallback when no meaningful error type is known.</summary>
    public const string OtherValue = "_OTHER";

    private static readonly string[] SourceKeys = ["ErrorType", "ExceptionType"];

    /// <inheritdoc />
    public override void OnEnd(LogRecord data)
    {
        var attributes = data.Attributes;
        if (attributes is null)
            return;

        foreach (var attribute in attributes)
        {
            if (!SourceKeys.Contains(attribute.Key))
                continue;

            string? value = attribute.Value?.ToString();
            data.Attributes = [.. attributes, new KeyValuePair<string, object?>(AttributeKey, string.IsNullOrWhiteSpace(value) ? OtherValue : value)];

            return;
        }
    }
}
