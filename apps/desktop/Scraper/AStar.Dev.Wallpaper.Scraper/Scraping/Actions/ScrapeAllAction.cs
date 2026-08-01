using AStar.Dev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStar.Dev.Wallpaper.Scraper.Scraping.Actions;

/// <summary>
///     Runs Search Categories, Top Wallpapers, and Subscriptions in sequence. Each step reports its own errors
///     via <paramref name="progress" /> and a failing step does not prevent the next step from running.
/// </summary>
public sealed class ScrapeAllAction(
    IScrapeAction searchCategoryScrapeAction,
    ITopWallpapersScrapeAction topWallpapersScrapeAction,
    ISubscriptionsScrapeAction subscriptionsScrapeAction) : IScrapeAllAction
{
    /// <inheritdoc />
    public string Name => "Scrape All Wallpapers";

    /// <inheritdoc />
    public async Task<Exceptional<UnitFp>> ExecuteAsync(IPage page, IProgress<string> progress, CancellationToken cancellationToken) =>
        await Try.RunAsync(async () =>
        {
            await RunStepAsync(searchCategoryScrapeAction, page, progress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await RunStepAsync(topWallpapersScrapeAction, page, progress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await RunStepAsync(subscriptionsScrapeAction, page, progress, cancellationToken);

            return UnitFp.Instance;
        }, cancellationToken);

    private static Task<UnitFp> RunStepAsync(IScrapeAction action, IPage page, IProgress<string> progress, CancellationToken cancellationToken) =>
        action.ExecuteAsync(page, progress, cancellationToken).MatchAsync(
            onSuccess: _ => UnitFp.Instance,
            onFailure: exception =>
            {
                progress.Report($"{action.Name}: {exception.Message}");

                return UnitFp.Instance;
            });
}
