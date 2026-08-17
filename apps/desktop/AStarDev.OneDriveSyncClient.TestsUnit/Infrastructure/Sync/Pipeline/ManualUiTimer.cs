using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

internal sealed class ManualUiTimer : IUiTimer
{
    private Action? callback;

    public void Start(TimeSpan interval, Action callback) => this.callback = callback;

    public void Tick() => callback?.Invoke();
}
