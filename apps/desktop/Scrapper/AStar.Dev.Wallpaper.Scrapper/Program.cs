using Avalonia;
using AStar.Dev.Wallpaper.Scrapper;

return AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
