using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

/// <summary>Drains sync jobs from a channel and executes each one.</summary>
public interface ISyncWorker
{
    /// <summary>Processes all jobs from <paramref name="reader"/> until the channel completes or <paramref name="ct"/> is cancelled.</summary>
    Task RunAsync(ChannelReader<SyncJob> reader, string accountId, Func<CancellationToken, Task<string>> tokenFactory, Func<SyncJob, bool, string?, Task> onJobComplete, CancellationToken ct);
}
