using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.FilesDb.Models;
using FileClassificationDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassification;
using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public interface IImportExportService
{
    void ExportFileClassificationsToFile(List<FileClassificationDomain> classifications);
    Result<List<FileClassificationDomain>, string> ImportFileClassificationsFromFile();
    void ExportScrapeConfigurationToFile(ScrapeConfigurationEntity entity);
    Result<ScrapeConfigurationEntity, string> ImportScrapeConfigurationFromFile();

    void ExportScrapedTagsToFile(List<ScrapedTagDomain> tags);
    Result<List<ScrapedTagDomain>, string> ImportScrapedTagsFromFile();
}
