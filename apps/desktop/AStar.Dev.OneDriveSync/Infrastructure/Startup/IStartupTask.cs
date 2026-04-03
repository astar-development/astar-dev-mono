using System.Threading;
using System.Threading.Tasks;

namespace AStar.Dev.OneDriveSync.Infrastructure.Startup;

public interface IStartupTask
{
    string Name { get; }
    Task RunAsync(CancellationToken ct);
}
