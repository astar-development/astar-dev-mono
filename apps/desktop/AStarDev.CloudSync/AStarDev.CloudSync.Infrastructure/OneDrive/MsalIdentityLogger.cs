using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;

namespace AStarDev.CloudSync.Infrastructure.OneDrive;

/// <summary>Forwards MSAL's own warnings and errors to the app log. MSAL's PII logging stays off, so its messages hold no account details.</summary>
/// <param name="logger">The app log MSAL messages go to.</param>
public sealed class MsalIdentityLogger(ILogger<MsalIdentityLogger> logger) : IIdentityLogger
{
    /// <inheritdoc />
    public bool IsEnabled(EventLogLevel eventLogLevel) => throw new NotImplementedException($"{logger}{eventLogLevel}");

    /// <inheritdoc />
    public void Log(LogEntry entry) => throw new NotImplementedException($"{entry}");
}
