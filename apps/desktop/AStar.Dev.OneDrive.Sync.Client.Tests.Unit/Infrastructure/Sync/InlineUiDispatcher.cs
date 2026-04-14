using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync;

internal sealed class InlineUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}
