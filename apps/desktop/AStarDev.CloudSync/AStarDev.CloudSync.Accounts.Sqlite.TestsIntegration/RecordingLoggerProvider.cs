using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Accounts.Sqlite.TestsIntegration;

public sealed class RecordingLoggerProvider : ILoggerProvider
{
    public RecordingLogger<RecordingLoggerProvider> Logger { get; } = new();

    public ILogger CreateLogger(string categoryName) => Logger;

    public void Dispose()
    {
    }
}
