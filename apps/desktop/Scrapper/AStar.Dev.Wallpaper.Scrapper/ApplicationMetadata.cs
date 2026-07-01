using System.Reflection;
using AStar.Dev.Utilities;
using Microsoft.VisualBasic.FileIO;

namespace AStar.Dev.Wallpaper.Scrapper;

public static class ApplicationMetadata
{
    public const string Name = "AStar.Dev.Wallpaper.Scrapper";
    public const string Version = "1.0.0";
    public const string Redacted = "REDACTED";

    public static string ApplicationFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!.CombinePath("..", "..", "..");

    public static string FileClassificationsExportFilePath => SpecialDirectories.MyDocuments.CombinePath("Scrapper", "FileClassifications.json");

    public static string ScrapeConfigurationExportFilePath => SpecialDirectories.MyDocuments.CombinePath("Scrapper", "ScrapeConfiguration.json");
    public static string ScrapedTagsExportFilePath => SpecialDirectories.MyDocuments.CombinePath("Scrapper", "ScrapedTags.json");
}
