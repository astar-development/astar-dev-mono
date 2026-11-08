using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class RecordingLoggerProvider : ILoggerProvider
{
    public RecordingLogger<RecordingLoggerProvider> Logger { get; } = new();

    public ILogger CreateLogger(string categoryName) => Logger;

    public void Dispose()
    {
    }
}
