using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Forwards MSAL's own warnings and errors to the app log. MSAL's PII logging stays off, and email addresses and GUIDs in the text are replaced with [redacted], because server error text can still name the account.</summary>
/// <param name="logger">The app log MSAL messages go to.</param>
public sealed partial class MsalIdentityLogger(ILogger<MsalIdentityLogger> logger) : IIdentityLogger
{
    private const string Redacted = "[redacted]";

    /// <inheritdoc />
    public bool IsEnabled(EventLogLevel eventLogLevel) => eventLogLevel is EventLogLevel.Critical or EventLogLevel.Error or EventLogLevel.Warning;

    /// <inheritdoc />
    public void Log(LogEntry entry)
    {
        if (!IsEnabled(entry.EventLogLevel))
            return;

        var level = ToLogLevel(entry.EventLogLevel);
        var message = Redact(entry.Message ?? string.Empty);
        AccountAddLog.MsalMessage(logger, level, message);
    }

    private static string Redact(string message) => GuidPattern().Replace(EmailPattern().Replace(message, Redacted), Redacted);

    private static LogLevel ToLogLevel(EventLogLevel eventLogLevel) => eventLogLevel switch
    {
        EventLogLevel.Critical => LogLevel.Critical,
        EventLogLevel.Error => LogLevel.Error,
        _ => LogLevel.Warning,
    };

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex GuidPattern();
}
