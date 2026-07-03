using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public interface IFileDetailRepository
{
    Task<bool> ExistsAsync(string fileName);
    Task AddAsync(FileDetailEntity fileDetail);
}
