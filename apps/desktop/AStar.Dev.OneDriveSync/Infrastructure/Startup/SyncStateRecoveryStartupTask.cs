using System.Threading;
using System.Threading.Tasks;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed class SyncStateRecoveryStartupTask : IStartupTask
{
    public const string TaskName = "SyncStateRecovery";

    string IStartupTask.Name => TaskName;

    public Task RunAsync(CancellationToken ct) => Task.CompletedTask;
}
