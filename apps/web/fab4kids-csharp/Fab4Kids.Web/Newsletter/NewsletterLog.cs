using Microsoft.Extensions.Logging;

namespace Fab4Kids.Web.Newsletter;

internal static partial class NewsletterLog
{
    [LoggerMessage(EventId = 6300, Level = LogLevel.Error, Message = "Checking for an existing newsletter subscriber failed: {Reason}")]
    public static partial void ExistsCheckFailed(ILogger logger, string reason);

    [LoggerMessage(EventId = 6301, Level = LogLevel.Error, Message = "Sending the newsletter confirmation email failed: {Reason}")]
    public static partial void ConfirmationEmailFailed(ILogger logger, string reason);

    [LoggerMessage(EventId = 6302, Level = LogLevel.Error, Message = "Storing the newsletter subscriber failed: {Reason}")]
    public static partial void SubscribeFailed(ILogger logger, string reason);

    [LoggerMessage(EventId = 6303, Level = LogLevel.Warning, Message = "Newsletter subscription rejected: consent was not given.")]
    public static partial void ConsentNotGiven(ILogger logger);

    [LoggerMessage(EventId = 6304, Level = LogLevel.Warning, Message = "Newsletter subscription rejected: the email address is invalid.")]
    public static partial void InvalidEmail(ILogger logger);

    [LoggerMessage(EventId = 6305, Level = LogLevel.Information, Message = "Newsletter subscription started.")]
    public static partial void SubscribeStarted(ILogger logger);

    [LoggerMessage(EventId = 6306, Level = LogLevel.Information, Message = "Newsletter subscriber already subscribed.")]
    public static partial void AlreadySubscribed(ILogger logger);

    [LoggerMessage(EventId = 6307, Level = LogLevel.Information, Message = "Newsletter subscriber stored.")]
    public static partial void Subscribed(ILogger logger);
}
