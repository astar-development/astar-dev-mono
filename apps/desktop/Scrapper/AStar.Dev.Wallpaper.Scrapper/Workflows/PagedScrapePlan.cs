using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Workflows;

/// <summary>Describes one workflow's configuration of the shared <see cref="PagedScrapeRunner" /> page loop.</summary>
/// <param name="StartingPage">The first page number to visit.</param>
/// <param name="TotalPages">The last page number to visit, inclusive.</param>
/// <param name="RecordProgress">Invoked with the current page number before the page is loaded, so the caller can record its own progress state.</param>
/// <param name="LoadPageAsync">Loads the page identified by the supplied page number.</param>
/// <param name="GetLinksAsync">Reads the image page links from the currently loaded page.</param>
/// <param name="ProcessLinksAsync">Processes the image page links read from the currently loaded page.</param>
public record PagedScrapePlan(
    int StartingPage,
    int TotalPages,
    Action<int> RecordProgress,
    Func<int, Task<Result<Unit, ScrapeError>>> LoadPageAsync,
    Func<Task<Result<IReadOnlyCollection<string>, ScrapeError>>> GetLinksAsync,
    Func<IReadOnlyCollection<string>, CancellationToken, Task<Result<Unit, ScrapeError>>> ProcessLinksAsync);
