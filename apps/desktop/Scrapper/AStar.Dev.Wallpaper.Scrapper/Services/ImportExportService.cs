using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Utilities;
using AStar.Dev.Wallpaper.Scrapper.DTOs;
using Serilog;
using System.IO.Abstractions;
using FileClassificationDto = AStar.Dev.Wallpaper.Scrapper.DTOs.FileClassification;
using FileClassificationDomain = AStar.Dev.Infrastructure.FilesDb.Models.FileClassification;
using ScrapedTagDto = AStar.Dev.Wallpaper.Scrapper.DTOs.ScrapedTag;
using ScrapedTagDomain = AStar.Dev.Infrastructure.FilesDb.Models.ScrapedTag;

namespace AStar.Dev.Wallpaper.Scrapper.Services;

public sealed class ImportExportService(IFileSystem fileSystem, TimeProvider timeProvider, ILogger logger) : IImportExportService
{
    public void ExportFileClassificationsToFile(List<FileClassificationDomain> classifications)
    {
        try
        {
            var json = classifications.ToDtos().ToJson();
            fileSystem.File.WriteAllText(ApplicationMetadata.FileClassificationsExportFilePath, json);
            logger.Information("Classifications exported to {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);
        }
        catch(Exception ex)
        {
            logger.Error(ex, "Failed to export classifications to file: {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);
            throw;
        }
    }

    public Result<List<FileClassificationDomain>, string> ImportFileClassificationsFromFile()
    {
        if(!fileSystem.File.Exists(ApplicationMetadata.FileClassificationsExportFilePath))
        {
            logger.Error("File not found: {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);

            return $"Error: File not found - {ApplicationMetadata.FileClassificationsExportFilePath}";
        }

        var classificationsJson = fileSystem.File.ReadAllText(ApplicationMetadata.FileClassificationsExportFilePath);
        var classifications = classificationsJson.FromJson<List<FileClassificationDto>>(AStar.Dev.Utilities.Constants.WebDeserialisationSettings);

        if(classifications is null)
        {
            logger.Error("Failed to deserialize classifications from file: {FilePath}", ApplicationMetadata.FileClassificationsExportFilePath);

            return $"Error: Failed to deserialize classifications from file - {ApplicationMetadata.FileClassificationsExportFilePath}";
        }

        return classifications.ToDomain();
    }

    public void ExportScrapeConfigurationToFile(ScrapeConfigurationEntity entity)
    {
        try
        {
            var json = entity.ToDto().ToJson();
            fileSystem.File.WriteAllText(ApplicationMetadata.ScrapeConfigurationExportFilePath, json);
            logger.Information("Scrape configuration exported to {FilePath}", ApplicationMetadata.ScrapeConfigurationExportFilePath);
        }
        catch(Exception ex)
        {
            logger.Error(ex, "Failed to export scrape configuration to file: {FilePath}", ApplicationMetadata.ScrapeConfigurationExportFilePath);
            throw;
        }
    }

    public void ExportScrapedTagsToFile(List<ScrapedTagDomain> tags)
    {
        try
        {
            var json = tags.ToDtos().ToJson();
            fileSystem.File.WriteAllText(ApplicationMetadata.ScrapedTagsExportFilePath, json);
            logger.Information("Tags exported to {FilePath}", ApplicationMetadata.ScrapedTagsExportFilePath);
        }
        catch(Exception ex)
        {
            logger.Error(ex, "Failed to export tags to file: {FilePath}", ApplicationMetadata.ScrapedTagsExportFilePath);
            throw;
        }
    }

    public Result<ScrapeConfigurationEntity, string> ImportScrapeConfigurationFromFile()
    {
        if(!fileSystem.File.Exists(ApplicationMetadata.ScrapeConfigurationExportFilePath))
        {
            logger.Error("File not found: {FilePath}", ApplicationMetadata.ScrapeConfigurationExportFilePath);

            return $"Error: File not found - {ApplicationMetadata.ScrapeConfigurationExportFilePath}";
        }

        var json = fileSystem.File.ReadAllText(ApplicationMetadata.ScrapeConfigurationExportFilePath);
        var dto = json.FromJson<ScrapeConfigurationDto>(AStar.Dev.Utilities.Constants.WebDeserialisationSettings);

        if(dto is null)
        {
            logger.Error("Failed to deserialize scrape configuration from file: {FilePath}", ApplicationMetadata.ScrapeConfigurationExportFilePath);

            return $"Error: Failed to deserialize scrape configuration from file - {ApplicationMetadata.ScrapeConfigurationExportFilePath}";
        }

        return dto.ToDomain();
    }

    public Result<List<ScrapedTagDomain>, string> ImportScrapedTagsFromFile()
    {
        if(!fileSystem.File.Exists(ApplicationMetadata.ScrapedTagsExportFilePath))
        {
            logger.Error("File not found: {FilePath}", ApplicationMetadata.ScrapedTagsExportFilePath);

            return $"Error: File not found - {ApplicationMetadata.ScrapedTagsExportFilePath}";
        }

        var tagsJson = fileSystem.File.ReadAllText(ApplicationMetadata.ScrapedTagsExportFilePath);
        var tags = tagsJson.FromJson<List<ScrapedTagDto>>(AStar.Dev.Utilities.Constants.WebDeserialisationSettings);

        if(tags is null)
        {
            logger.Error("Failed to deserialize tags from file: {FilePath}", ApplicationMetadata.ScrapedTagsExportFilePath);

            return $"Error: Failed to deserialize tags from file - {ApplicationMetadata.ScrapedTagsExportFilePath}";
        }

        return tags.ToDomain(timeProvider);
    }
}
