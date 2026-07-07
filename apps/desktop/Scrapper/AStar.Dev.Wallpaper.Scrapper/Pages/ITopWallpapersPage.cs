using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public interface ITopWallpapersPage
{
    Task<Result<Unit, ScrapeError>> LoadTopWallpapersPageAsync(int pageNumber);

    Task<Result<int, ScrapeError>> PageInfoAsync();

    Task<Result<IReadOnlyCollection<string>, ScrapeError>> GetImagePageLinksAsync();
}
