using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Sync.Engine.Features.Concurrency;
using AStar.Dev.Sync.Engine.Features.SyncOrchestration;
using AStar.Dev.Sync.Engine.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Sync.Engine.Features.Scheduling;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI in SyncEngineServiceExtensions.")]
internal sealed class SyncScheduler(ISyncEngine syncEngine, SyncGate syncGate, ILogger<SyncScheduler> logger) : ISyncScheduler
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(15);

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken ct = default)
    {
        using var timer = new PeriodicTimer(DefaultInterval);

        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            if (syncGate.IsAnyAccountSyncing())
            {
                SyncEngineLogMessage.SyncPaused(logger, "scheduled");
                continue;
            }

            _ = syncEngine.StartSyncAsync("scheduled", ct: ct);
        }
    }
}
