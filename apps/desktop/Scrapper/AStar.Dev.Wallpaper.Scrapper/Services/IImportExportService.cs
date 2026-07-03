using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using FileClassificationDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;
using ScrapedTagDomain = AStar.Dev.Infrastructure.AppDb.Entities.ScrapedTagEntity;

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
