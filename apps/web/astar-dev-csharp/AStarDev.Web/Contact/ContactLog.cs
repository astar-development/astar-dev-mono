using Microsoft.Extensions.Logging;

namespace AStarDev.Web.Contact;

internal static partial class ContactLog
{
    [LoggerMessage(EventId = 6210, Level = LogLevel.Warning, Message = "Contact form honeypot triggered by client {ClientHash}.")]
    public static partial void HoneypotTriggered(ILogger logger, string clientHash);

    [LoggerMessage(EventId = 6211, Level = LogLevel.Warning, Message = "Contact form rate limit exceeded by client {ClientHash}.")]
    public static partial void RateLimitExceeded(ILogger logger, string clientHash);

    [LoggerMessage(EventId = 6212, Level = LogLevel.Debug, Message = "Contact form validation failed for {InvalidProperties}.")]
    public static partial void ValidationFailed(ILogger logger, string invalidProperties);

    [LoggerMessage(EventId = 6213, Level = LogLevel.Information, Message = "Contact message send started.")]
    public static partial void SendStarted(ILogger logger);

    [LoggerMessage(EventId = 6214, Level = LogLevel.Information, Message = "Contact message sent.")]
    public static partial void SendSucceeded(ILogger logger);
}
