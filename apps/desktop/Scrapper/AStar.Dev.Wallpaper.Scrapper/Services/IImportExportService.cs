using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Entities;
using FileClassificationDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationCategoryEntity;
using FileClassificationKeywordDomain = AStar.Dev.Infrastructure.AppDb.Entities.FileClassificationKeywordEntity;
using ScrapedTagDomain = AStar.Dev.Infrastructure.AppDb.Entities.ScrapedTagEntity;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public interface IImportExportService
{
    void ExportFileClassificationsToFile((List<FileClassificationDomain> Categories, List<FileClassificationKeywordDomain> Keywords) classifications);
    Result<(List<FileClassificationDomain> Categories, List<FileClassificationKeywordDomain> Keywords), string> ImportFileClassificationsFromFile();
    void ExportScrapeConfigurationToFile(ScrapeConfigurationEntity entity);
    Result<ScrapeConfigurationEntity, string> ImportScrapeConfigurationFromFile();

    void ExportScrapedTagsToFile(List<ScrapedTagDomain> tags);
    Result<List<ScrapedTagDomain>, string> ImportScrapedTagsFromFile();
}
