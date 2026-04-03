using System.Threading;
using System.Threading.Tasks;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public sealed class TokenValidationStartupTask : IStartupTask
{
    public const string TaskName = "TokenValidation";

    string IStartupTask.Name => TaskName;

    public Task RunAsync(CancellationToken ct) => Task.CompletedTask;
}
