
namespace AStar.Dev.Wallpaper.Scrapper.Models;

public record ScrapeDirectories(string RootDirectory, string BaseSaveDirectory, string BaseDirectory, string BaseDirectoryFamous, string SubDirectoryName)
{
    internal Infrastructure.AppDb.Entities.ScrapeDirectoriesEntity ToEntity() => new()
    {
        RootDirectory = RootDirectory,
        BaseSaveDirectory = BaseSaveDirectory,
        BaseDirectory = BaseDirectory,
        BaseDirectoryFamous = BaseDirectoryFamous,
        SubDirectoryName = SubDirectoryName
    };
}
