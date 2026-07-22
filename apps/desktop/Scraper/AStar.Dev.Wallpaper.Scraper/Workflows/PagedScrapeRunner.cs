using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Guard.Clauses;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Support;

namespace AStar.Dev.Wallpaper.Scraper.Workflows;

/// <summary>Runs the page loop shared by every paged scrape workflow: delay, record progress, save configuration, load the page, read its links, then process them.</summary>
public sealed class PagedScrapeRunner(ConfigurationSaver configurationSaver, IDelayStrategy delayStrategy)
{
    /// <summary>Runs <paramref name="plan" /> from its starting page to its total page, inclusive, stopping at the first failing step.</summary>
    public async Task<Result<Unit, ScrapeError>> RunAsync(PagedScrapePlan plan, CancellationToken cancellationToken = default)
    {
        GuardAgainst.Null(plan);

        for (int pageNumber = plan.StartingPage; pageNumber <= plan.TotalPages; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await delayStrategy.DelayAsync(DelayKind.PageNavigation, cancellationToken).ConfigureAwait(false);
            plan.RecordProgress(pageNumber);

            var pageResult = await configurationSaver.SaveUpdatedConfigurationAsync(cancellationToken)
                .BindAsync(_ => plan.LoadPageAsync(pageNumber))
                .BindAsync(_ => plan.GetLinksAsync())
                .BindAsync(links => plan.ProcessLinksAsync(links, cancellationToken))
                .ConfigureAwait(false);

            bool pageFailed = pageResult.Match(_ => false, _ => true);

            if (pageFailed) return pageResult;
        }

        return Unit.Instance;
    }
}
