using System.Diagnostics.CodeAnalysis;

namespace AStar.Dev.Sync.Engine.Features.Activity;

/// <summary>No-op implementation used when no activity feed consumer is registered.</summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI in SyncEngineServiceExtensions.")]
internal sealed class NullActivityReporter : IActivityReporter
{
    /// <inheritdoc />
    public void Report(string accountId, ActivityActionType actionType, string filePath) { }
}
