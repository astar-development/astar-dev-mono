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
}
