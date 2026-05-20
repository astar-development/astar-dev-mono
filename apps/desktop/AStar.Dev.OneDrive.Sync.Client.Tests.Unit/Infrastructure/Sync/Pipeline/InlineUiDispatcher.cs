using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

internal sealed class InlineUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => action();
}
