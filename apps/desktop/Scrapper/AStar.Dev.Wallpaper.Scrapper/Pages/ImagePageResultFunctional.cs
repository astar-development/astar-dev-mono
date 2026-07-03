
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scrapper.Models;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scrapper.Pages;

public interface IImagePageResultFunctional
{
    Task<Result<Unit, string>> GetImagePagesAsync(Logger logger);
}

public class ImagePageResultFunctional(IDbContextFactory<AppDbContext> dbContextFactory) : IImagePageResultFunctional
{
    /// <inheritdoc />
    public async Task<Result<Unit, string>> GetImagePagesAsync(Logger logger)
    {
        using var ctx = dbContextFactory.CreateDbContext();
        var scrapeConfiguration = ctx.ScrapeConfiguration.GetScrapeConfigurations().ToAppModel();

        logger.Information("BaseDirectory: {BaseDirectory}", scrapeConfiguration.ScrapeDirectories.BaseDirectory);
        logger.Information("Image pages retrieved.");

        return Unit.Value;
    }
}
