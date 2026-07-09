using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Models;

namespace AStar.Dev.Wallpaper.Scraper.Pages;

public interface ITopWallpapersPage
{
    Task<Result<Unit, ScrapeError>> LoadTopWallpapersPageAsync(int pageNumber);

    Task<Result<int, ScrapeError>> PageInfoAsync();

    Task<Result<IReadOnlyCollection<string>, ScrapeError>> GetImagePageLinksAsync();
}
