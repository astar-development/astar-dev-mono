namespace AStar.Dev.Wallpaper.Scrapper.Models;

public sealed class Logging
{
    public Loglevel? LogLevel { get; set; }
    public Console? Console { get; set; }
    public Serilog? Serilog { get; set; }
}
