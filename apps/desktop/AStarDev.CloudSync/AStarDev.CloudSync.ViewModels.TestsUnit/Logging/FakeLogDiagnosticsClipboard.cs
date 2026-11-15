using AStarDev.CloudSync.ViewModels.Logging;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Logging;

public sealed class FakeLogDiagnosticsClipboard : ILogDiagnosticsClipboard
{
    public List<string> Copied { get; } = [];

    public Task SetTextAsync(string text, CancellationToken cancellationToken)
    {
        Copied.Add(text);

        return Task.CompletedTask;
    }
}
