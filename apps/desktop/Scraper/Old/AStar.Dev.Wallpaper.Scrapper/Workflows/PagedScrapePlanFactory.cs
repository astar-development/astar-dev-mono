using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Guard.Clauses;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

/// <summary>Factory methods for creating instances of <see cref="PagedScrapePlan" />.</summary>
public static class PagedScrapePlanFactory
{
    /// <summary>Creates a <see cref="PagedScrapePlan" />.</summary>
    public static PagedScrapePlan Create(
        int startingPage,
        int totalPages,
        Action<int> recordProgress,
        Func<int, Task<Result<Unit, ScrapeError>>> loadPageAsync,
        Func<Task<Result<IReadOnlyCollection<string>, ScrapeError>>> getLinksAsync,
        Func<IReadOnlyCollection<string>, CancellationToken, Task<Result<Unit, ScrapeError>>> processLinksAsync)
    {
        GuardAgainst.Null(recordProgress);
        GuardAgainst.Null(loadPageAsync);
        GuardAgainst.Null(getLinksAsync);
        GuardAgainst.Null(processLinksAsync);

        return new(startingPage, totalPages, recordProgress, loadPageAsync, getLinksAsync, processLinksAsync);
    }
}
