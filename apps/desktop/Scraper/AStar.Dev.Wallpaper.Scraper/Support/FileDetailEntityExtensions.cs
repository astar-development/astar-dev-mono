using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scraper.Support;

public static class FileDetailEntityExtensions
{
    public static string FullNameWithPath(this FileDetailEntity fileDetail) => fileDetail.DirectoryName.Value.CombinePath(fileDetail.FileName.Value);
}
