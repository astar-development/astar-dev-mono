namespace AStar.Dev.Wallpaper.Scrapper.DTOs;

public sealed record ConnectionStringsDto
{
    public string Sqlite { get; init; } = string.Empty;
}
