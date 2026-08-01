namespace AStar.Dev.Wallpaper.Scrapper.Models;

public sealed class Formatteroptions
{
    public bool SingleLine { get; set; }
    public bool IncludeScopes { get; set; }
    public string? TimestampFormat { get; set; }
    public bool UseUtcTimestamp { get; set; }
    public Jsonwriteroptions? JsonWriterOptions { get; set; }
}
