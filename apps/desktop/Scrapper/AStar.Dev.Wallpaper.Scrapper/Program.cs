using AStar.Dev.Wallpaper.Scrapper;
using Avalonia;

return AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
