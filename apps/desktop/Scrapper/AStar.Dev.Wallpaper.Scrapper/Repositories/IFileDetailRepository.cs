using AStar.Dev.Infrastructure.FilesDb.Models;

namespace AStar.Dev.Wallpaper.Scrapper.Repositories;

public interface IFileDetailRepository
{
    Task<bool> ExistsAsync(string fileName);
    Task AddAsync(FileDetail fileDetail);
}
