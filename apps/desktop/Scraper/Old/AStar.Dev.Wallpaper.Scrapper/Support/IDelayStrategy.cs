namespace AStar.Dev.Wallpaper.Scrapper.Support;

/// <summary>Injects the non-deterministic delays a scrape workflow waits on, so the workflow itself stays testable without real waits.</summary>
public interface IDelayStrategy
{
    /// <summary>Delays for the duration appropriate to <paramref name="delayKind" />.</summary>
    Task DelayAsync(DelayKind delayKind, CancellationToken cancellationToken = default);
}
