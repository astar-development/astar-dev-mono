using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.TestData;

internal sealed class NoOpDelayStrategy : IDelayStrategy
{
    public Task DelayAsync(DelayKind delayKind, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
