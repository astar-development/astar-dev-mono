using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using FileClassificationDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;
using FileClassificationKeywordDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationKeywordEntity;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public interface IImportExportService
{
    void ExportFileClassificationsToFile((List<FileClassificationDomain> Categories, List<FileClassificationKeywordDomain> Keywords) classifications);
    Result<(List<FileClassificationDomain> Categories, List<FileClassificationKeywordDomain> Keywords), ScrapeError> ImportFileClassificationsFromFile();
    void ExportScrapeConfigurationToFile(ScrapeConfigurationEntity entity);
    Result<ScrapeConfigurationEntity, ScrapeError> ImportScrapeConfigurationFromFile();

    void ExportScrapedTagsToFile(List<FileClassificationCategoryEntity> tags);
    Result<List<FileClassificationCategoryEntity>, ScrapeError> ImportScrapedTagsFromFile();
}
