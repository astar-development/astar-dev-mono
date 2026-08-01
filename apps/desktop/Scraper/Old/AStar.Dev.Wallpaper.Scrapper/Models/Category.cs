namespace AStar.Dev.Wallpaper.Scrapper.Models;

public sealed class Category
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int LastKnownImageCount { get; set; }

    public int LastPageVisited { get; set; }

    public int TotalPages { get; set; }

    public bool IsUpToDate(int imageCount, int pageCount) => imageCount == LastKnownImageCount && pageCount == TotalPages;
}
