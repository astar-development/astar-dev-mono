using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;

namespace AStar.Dev.Wallpaper.Scraper.Repositories;

public interface IFileDetailRepository
{
    Task<Result<bool, ScrapeError>> ExistsAsync(string fileName, CancellationToken cancellationToken);
    Task<Result<Unit, ScrapeError>> AddAsync(FileDetailEntity fileDetail, CancellationToken cancellationToken);
}
