using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AStarDev.ControlDb.ScrapeConfiguration;
using Avalonia;
using Avalonia.ReactiveUI;
using Velopack;

namespace AStarDev.WallpaperScraper;

[ExcludeFromCodeCoverage]
internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        string data = File.ReadAllText("/run/media/jbarden/Tbdrive/categories.json");
        var categories = System.Text.Json.JsonSerializer.Deserialize<List<TestCategory>>(data);
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}

public class TestCategory
{
    public string SearchConfigurationId { get; set; } = string.Empty;

    /// <summary>The unique identifier for the search category.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The human-readable name of the category.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The number of images observed for this category as of the last scrape.</summary>
    public int LastKnownImageCount { get; set; }

    /// <summary>The last page visited for this category.</summary>
    public int LastPageVisited { get; set; }

    /// <summary>The total number of pages available for this category.</summary>
    public int TotalPages { get; set; }

    /// <summary>Whether this category should be included in the scraping process.</summary>
    [JsonConverter(typeof(NumericBooleanJsonConverter))]
    public bool IncludeInSearch { get; set; }

    /// <summary>
    ///   Whether this category defines a famous person. This flag can be used to prioritize or filter categories based on their significance or popularity.
    /// </summary>
    [JsonConverter(typeof(NumericBooleanJsonConverter))]
    public bool IsFamous { get; set; }

    /// <summary>
    ///  Whether this category defines the internet classification. This flag can be used to determine if the category is relevant for internet-based searches or operations.
    /// </summary>
    [JsonConverter(typeof(NumericBooleanJsonConverter))]
    public bool IsInternet { get; set; }
}
