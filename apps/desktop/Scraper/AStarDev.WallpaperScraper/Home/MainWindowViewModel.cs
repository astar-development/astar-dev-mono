using System.Reflection;
using AStar.Dev.Logging.Extensions;
using AStarDev.Utilities;
using AStarDev.WallpaperScraper.Configuration;
using AStarDev.WallpaperScraper.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;

namespace AStarDev.WallpaperScraper.Home;

public class MainWindowViewModel : ReactiveObject
{
    private readonly IPlaywrightService playwrightService;

    public MainWindowViewModel(IOptions<ScrapeConfiguration> scrapeConfiguration, IPlaywrightService playwrightService, ILogger<MainWindow> logger)
    {
        string scrapeConfig = scrapeConfiguration.Value.ToJson();
        LogMessage.Information(logger, "MainWindowViewModel initialized with ScrapeConfiguration: {ScrapeConfiguration}", scrapeConfig);
        Title = $"{scrapeConfiguration.Value.ApplicationName} V{ApplicationVersion}";
        SetWindowSize(scrapeConfiguration.Value.WindowSize);
        this.playwrightService = playwrightService;
    }

    public string Title { get; }
    public double WindowWidth { get; set; } = 1_000;
    public double WindowHeight { get; set; } = 1_000;

    /// <summary>
    ///     The version CI stamps from the release tag (-p:Version=...), so the title can
    ///     never drift from the Velopack package version. SourceLink appends +sha; strip it.
    /// </summary>
    public static string ApplicationVersion { get; } = typeof(MainWindowViewModel).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? "0.0.0";

    private void SetWindowSize(WindowSize windowSize)
    {
        WindowWidth = windowSize.Width;
        WindowHeight = windowSize.Height;
    }
}
