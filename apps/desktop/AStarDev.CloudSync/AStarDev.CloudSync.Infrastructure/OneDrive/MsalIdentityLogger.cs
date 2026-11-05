using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Forwards MSAL's own warnings and errors to the app log. MSAL's PII logging stays off, so its messages hold no account details.</summary>
/// <param name="logger">The app log MSAL messages go to.</param>
public sealed class MsalIdentityLogger(ILogger<MsalIdentityLogger> logger) : IIdentityLogger
{
    /// <inheritdoc />
    public bool IsEnabled(EventLogLevel eventLogLevel) => eventLogLevel is EventLogLevel.Critical or EventLogLevel.Error or EventLogLevel.Warning;

    /// <inheritdoc />
    public void Log(LogEntry entry)
    {
        if (!IsEnabled(entry.EventLogLevel))
            return;

        var level = ToLogLevel(entry.EventLogLevel);
        AccountAddLog.MsalMessage(logger, level, entry.Message ?? string.Empty);
    }

    private static LogLevel ToLogLevel(EventLogLevel eventLogLevel) => eventLogLevel switch
    {
        EventLogLevel.Critical => LogLevel.Critical,
        EventLogLevel.Error => LogLevel.Error,
        _ => LogLevel.Warning,
    };
}
