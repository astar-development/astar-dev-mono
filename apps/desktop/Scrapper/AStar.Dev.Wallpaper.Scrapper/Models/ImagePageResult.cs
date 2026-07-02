namespace AStar.Dev.Wallpaper.Scrapper.Models;

public record ImagePageResult(string? ImageUrl, List<string> DirectoryName, string FilePrefix, bool Skip, IReadOnlyList<string> Tags);
