using System.Globalization;
using AStarDev.LoggingOTel;

namespace AStarDev.LoggingExtensions;

/// <summary>Produces low-cardinality <c>error.type</c> values for failed operations: the exception's full type name, <see cref="Timeout" />, an HTTP status code or <see cref="Other" />.</summary>
public static class ErrorTypes
{
    /// <summary>The value for an operation that timed out.</summary>
    public const string Timeout = "timeout";

    /// <summary>The semantic-convention fallback when no meaningful error type is known.</summary>
    public const string Other = ErrorTypeLogProcessor.OtherValue;

    /// <summary>
    ///     Describes a failure caused by an exception. A <see cref="TimeoutException" />, or an <see cref="OperationCanceledException" /> whose direct inner
    ///     exception is one (an HttpClient timeout), gives <see cref="Timeout" />. Deeper nesting is not unwrapped. Any other exception gives its full
    ///     type name, and no exception gives <see cref="Other" />.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>The error type value.</returns>
    public static string From(Exception? exception) => exception switch
    {
        null => Other,
        TimeoutException or OperationCanceledException { InnerException: TimeoutException } => Timeout,
        _ => exception.ErrorType(),
    };

    /// <summary>Describes a failure reported by an HTTP status code.</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>The status code as invariant text, for example <c>503</c>.</returns>
    public static string FromStatusCode(int statusCode) => statusCode.ToString(CultureInfo.InvariantCulture);
}
