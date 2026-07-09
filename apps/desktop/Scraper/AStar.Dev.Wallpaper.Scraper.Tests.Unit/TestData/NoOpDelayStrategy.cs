using AStar.Dev.Wallpaper.Scraper.Support;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.TestData;

internal sealed class NoOpDelayStrategy : IDelayStrategy
{
    public Task DelayAsync(DelayKind delayKind, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
