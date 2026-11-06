namespace AStarDev.LoggingExtensions;

/// <summary>Provides the PII-free exception detail that may be logged (LG-02): the type name, never the message, stack trace or instance.</summary>
public static class ExceptionExtensions
{
    /// <summary>Gets the low-cardinality error type of the exception, its full type name, for use as a log attribute.</summary>
    /// <param name="exception">The exception to describe.</param>
    /// <returns>The full type name of the exception, for example <c>System.InvalidOperationException</c>.</returns>
    public static string ErrorType(this Exception exception) => exception.GetType().FullName ?? exception.GetType().Name;
}
