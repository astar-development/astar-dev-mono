using Microsoft.Extensions.Logging;

namespace AStarDev.LoggingExtensions;

/// <summary>Provides extension methods for logging common application events, such as startup, shutdown, and errors, using strongly-typed log messages with structured logging support.</summary>
public static partial class LogMessage
{
    /// <summary>Logs an informational message indicating that a specific page has been viewed.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="pageName">The name of the page that was viewed.</param>
    [LoggerMessage(EventId = 200, Level = LogLevel.Information, Message = "Page `{PageName}` viewed.")]
    public static partial void PageView(ILogger logger, string pageName);

    /// <summary>Logs a debug message for the specified location.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="location">The location of the event.</param>
    /// <param name="message">The debug message to log.</param>
    [LoggerMessage(EventId = 100, Level = LogLevel.Debug, Message = "{Location} has raised: `{Message}`.")]

    public static partial void Debug(ILogger logger, string location, string message);
    /// <summary>Logs a debug message for the specified location.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="message">The debug message to log.</param>
    [LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = "Message: {Message}")]
    public static partial void Debug(ILogger logger, string message);

    /// <summary>Logs an informational message for the specified location.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="message">The message to log.</param>
    [LoggerMessage(EventId = 203, Level = LogLevel.Information, Message = "{Message}.")]
    public static partial void Information(ILogger logger, string message);

    /// <summary>Logs an informational message for the specified location.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="location">The location of the event.</param>
    /// <param name="message">The information message to log.</param>
    [LoggerMessage(EventId = 201, Level = LogLevel.Information, Message = "{Location} has raised: `{Message}`.")]
    public static partial void Information(ILogger logger, string location, string message);

    /// <summary>Logs an informational message for the specified location.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="location">The location of the event.</param>
    /// <param name="message">The information message to log.</param>
    /// <param name="additionalInformation">The additional Information to log.</param>
    [LoggerMessage(EventId = 202, Level = LogLevel.Information, Message = "{Location} has raised: `{Message}` with additional info: `{AdditionalInformation}`.")]
    public static partial void Information(ILogger logger, string location, string message, string additionalInformation);

    /// <summary>Logs a warning message for the specified location.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="location">The location of the event.</param>
    /// <param name="message">The warning message to log.</param>
    [LoggerMessage(EventId = 400, Level = LogLevel.Warning, Message = "{Location} has raised: `{Message}`.")]
    public static partial void Warning(ILogger logger, string location, string message);

    /// <summary>Logs that an exception was caught, recording only the exception type name; the message, stack trace and instance are never logged (LG-02).</summary>
    /// <param name="logger">The logger used to log the event.</param>
    /// <param name="location">The location of the event.</param>
    /// <param name="errorType">The full type name of the exception, from <see cref="ExceptionExtensions.ErrorType" />.</param>
    [LoggerMessage(EventId = 5010, EventName = "exception", Level = LogLevel.Error, Message = "{Location} encountered {ErrorType}")]
    public static partial void ExceptionCaught(ILogger logger, string location, string errorType);

    /// <summary>Logs a warning message for a Bad Request (400) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Bad Request.</param>
    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "Bad Request (400) for `{Path}`")]
    public static partial void BadRequest(ILogger logger, string path);

    /// <summary>Logs a warning message for an Unauthorized (401) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Unauthorized access.</param>
    [LoggerMessage(EventId = 401, Level = LogLevel.Warning, Message = "Unauthorized (401) for `{Path}`")]
    public static partial void Unauthorized(ILogger logger, string path);

    /// <summary>Logs a warning message for a Forbidden (403) event, including the requested path; the user is never logged (LG-02).</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Forbidden error.</param>
    [LoggerMessage(EventId = 403, Level = LogLevel.Warning, Message = "Forbidden (403) for `{Path}`")]
    public static partial void Forbidden(ILogger logger, string path);

    /// <summary>Logs a warning message for a Not Found (404) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that resulted in the Not Found status.</param>
    [LoggerMessage(EventId = 404, Level = LogLevel.Warning, Message = "Not Found (404) for `{Path}`")]
    public static partial void NotFound(ILogger logger, string path);

    /// <summary>Logs a warning message for a Conflict (409) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Conflict.</param>
    [LoggerMessage(EventId = 409, Level = LogLevel.Warning, Message = "Conflict (409) for `{Path}`")]
    public static partial void Conflict(ILogger logger, string path);

    /// <summary>Logs a warning message for an Unprocessable Entity (422) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Unprocessable Entity.</param>
    [LoggerMessage(EventId = 422, Level = LogLevel.Warning, Message = "Unprocessable Entity (422) for `{Path}`")]
    public static partial void UnprocessableEntity(ILogger logger, string path);

    /// <summary>Logs a warning message for a Too Many Requests (429) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Too Many Requests.</param>
    [LoggerMessage(EventId = 429, Level = LogLevel.Warning, Message = "Too Many Requests (429) for `{Path}`")]
    public static partial void TooManyRequests(ILogger logger, string path);

    /// <summary>Logs an application error event caused by an exception (EventId 507), recording only the exception type name (LG-02).</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="errorMessage">The error message to log; never the exception message.</param>
    /// <param name="errorType">The full type name of the exception, from <see cref="ExceptionExtensions.ErrorType" />.</param>
    [LoggerMessage(EventId = 507, Level = LogLevel.Error, Message = "Error occurred : `{ErrorMessage}` ({ErrorType})")]
    public static partial void Error(ILogger logger, string errorMessage, string errorType);

    /// <summary>Logs an application error event (EventId 506).</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="errorMessage">The error message to log.</param>
    [LoggerMessage(EventId = 506, Level = LogLevel.Error, Message = "Error occurred : `{ErrorMessage}`")]
    public static partial void Error(ILogger logger, string errorMessage);

    /// <summary>Logs an internal server error event (EventId 500).</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Internal Server Error.</param>
    [LoggerMessage(EventId = 500, Level = LogLevel.Error, Message = "Internal Server Error (500) for `{Path}`")]
    public static partial void InternalServerError(ILogger logger, string path);

    /// <summary>Logs an error message for a Bad Gateway (502) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Bad Gateway error.</param>
    [LoggerMessage(EventId = 502, Level = LogLevel.Error, Message = "Bad Gateway (502) for `{Path}`")]
    public static partial void BadGateway(ILogger logger, string path);

    /// <summary>Logs an error message for a Service Unavailable (503) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Service Unavailable error.</param>
    [LoggerMessage(EventId = 503, Level = LogLevel.Error, Message = "Service Unavailable (503) for `{Path}`")]
    public static partial void ServiceUnavailable(ILogger logger, string path);

    /// <summary>Logs an error message for a Gateway Timeout (504) event, including the requested path.</summary>
    /// <param name="logger">The logger to be used for logging the event.</param>
    /// <param name="path">The path of the HTTP request that caused the Gateway Timeout.</param>
    [LoggerMessage(EventId = 504, Level = LogLevel.Error, Message = "Gateway Timeout (504) for `{Path}`")]
    public static partial void GatewayTimeout(ILogger logger, string path);

    /// <summary>Logs a Graph API throttle event with retry details (EH-02).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="attempt">Current retry attempt (1-based).</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="delaySeconds">Delay in seconds before the next retry.</param>
    [LoggerMessage(EventId = 4291, Level = LogLevel.Debug, Message = "Graph API throttled (attempt {Attempt}/{MaxRetries}), retrying after {DelaySeconds}s")]
    public static partial void GraphApiThrottled(ILogger logger, int attempt, int maxRetries, double delaySeconds);

    /// <summary>Logs the current memory usage of the application in megabytes, providing insights into resource consumption for debugging and performance monitoring purposes.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="usedMemoryMB">The amount of memory used in megabytes.</param>
    [LoggerMessage(EventId = 104, Level = LogLevel.Debug, Message = "Memory usage: {UsedMemoryMB:F2} MB")]
    public static partial void MemoryUsage(ILogger logger, double usedMemoryMB);
}
