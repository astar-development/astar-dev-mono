using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Support;

public class ScrapeConfigurationManager
{
    private readonly IDbContextFactory<AppDbContext> contextFactory;

    public ScrapeConfiguration Current { get; private set; }

    public ScrapeConfigurationManager(IDbContextFactory<AppDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;

        using var context = contextFactory.CreateDbContext();
        Current = context.ScrapeConfiguration.GetScrapeConfigurations().ToAppModel();
    }

    public virtual async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        Current = context.ScrapeConfiguration.GetScrapeConfigurations().ToAppModel();
    }
}
