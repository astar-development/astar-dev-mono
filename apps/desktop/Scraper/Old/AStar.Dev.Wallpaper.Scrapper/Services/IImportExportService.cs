using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using FileClassificationDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;
using FileClassificationKeywordDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationKeywordEntity;
using ScrapedTagDomain = AStar.Dev.Infrastructure.AppDb.Entities.ScrapedTagEntity;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public interface IImportExportService
{
    void ExportFileClassificationsToFile((List<FileClassificationDomain> Categories, List<FileClassificationKeywordDomain> Keywords) classifications);
    Result<(List<FileClassificationDomain> Categories, List<FileClassificationKeywordDomain> Keywords), ScrapeError> ImportFileClassificationsFromFile();
    void ExportScrapeConfigurationToFile(ScrapeConfigurationEntity entity);
    Result<ScrapeConfigurationEntity, ScrapeError> ImportScrapeConfigurationFromFile();

    void ExportScrapedTagsToFile(List<ScrapedTagDomain> tags);
    Result<List<ScrapedTagDomain>, ScrapeError> ImportScrapedTagsFromFile();
}
