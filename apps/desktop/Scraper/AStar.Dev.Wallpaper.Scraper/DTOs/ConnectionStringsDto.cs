namespace AStar.Dev.Wallpaper.Scraper.DTOs;

public sealed record ConnectionStringsDto
{
    public string Sqlite { get; init; } = string.Empty;
}
