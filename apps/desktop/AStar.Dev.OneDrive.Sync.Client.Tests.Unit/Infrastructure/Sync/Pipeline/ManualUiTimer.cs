using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;

internal sealed class ManualUiTimer : IUiTimer
{
    private Action? callback;

    public void Start(TimeSpan interval, Action callback) => this.callback = callback;

    public void Tick() => callback?.Invoke();
}
