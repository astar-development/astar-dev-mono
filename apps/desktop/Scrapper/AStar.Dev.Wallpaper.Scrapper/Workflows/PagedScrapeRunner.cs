using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Guard.Clauses;
using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

/// <summary>Runs the page loop shared by every paged scrape workflow: delay, record progress, save configuration, load the page, read its links, then process them.</summary>
public sealed class PagedScrapeRunner(ConfigurationSaver configurationSaver, IDelayStrategy delayStrategy)
{
    /// <summary>Runs <paramref name="plan" /> from its starting page to its total page, inclusive, stopping at the first failing step.</summary>
    public async Task<Result<Unit, ScrapeError>> RunAsync(PagedScrapePlan plan, CancellationToken ct = default)
    {
        GuardAgainst.Null(plan);

        for (int pageNumber = plan.StartingPage; pageNumber <= plan.TotalPages; pageNumber++)
        {
            ct.ThrowIfCancellationRequested();
            await delayStrategy.DelayAsync(DelayKind.PageNavigation, ct).ConfigureAwait(false);
            plan.RecordProgress(pageNumber);

            var pageResult = await configurationSaver.SaveUpdatedConfigurationAsync()
                .BindAsync(_ => plan.LoadPageAsync(pageNumber))
                .BindAsync(_ => plan.GetLinksAsync())
                .BindAsync(links => plan.ProcessLinksAsync(links, ct))
                .ConfigureAwait(false);

            var pageFailed = pageResult.Match(_ => false, _ => true);

            if (pageFailed) return pageResult;
        }

        return Unit.Value;
    }
}
