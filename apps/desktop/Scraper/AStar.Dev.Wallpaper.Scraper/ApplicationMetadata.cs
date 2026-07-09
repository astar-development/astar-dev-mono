using System.Reflection;
using AStar.Dev.Utilities;

namespace AStar.Dev.Wallpaper.Scraper;

public static class ApplicationMetadata
{
    public const string Name = "AStar.Dev.Wallpaper.Scraper";
    public const string Version = "1.0.0";
    public const string Redacted = "REDACTED";

    public static string ApplicationFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!.CombinePath("..", "..", "..");

    public static string FileClassificationsExportFilePath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).CombinePath("Scraper", "FileClassifications.json");

    public static string ScrapeConfigurationExportFilePath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).CombinePath("Scraper", "ScrapeConfiguration.json");
    public static string ScrapedTagsExportFilePath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).CombinePath("Scraper", "ScrapedTags.json");
}
