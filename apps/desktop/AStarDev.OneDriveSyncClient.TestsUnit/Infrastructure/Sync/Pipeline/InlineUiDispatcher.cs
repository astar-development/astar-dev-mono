using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

internal sealed class InlineUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}
