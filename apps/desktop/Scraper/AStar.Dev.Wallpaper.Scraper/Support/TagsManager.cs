using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Support;

public sealed class TagsManager
{
    public TagsToIgnoreCompletely TagsToIgnoreCompletely { get; }
    public TagsTextToIgnore TagsTextToIgnore { get; }

    public TagsManager(IDbContextFactory<AppDbContext> contextFactory)
    {
        using var context = contextFactory.CreateDbContext();

        TagsToIgnoreCompletely = TagsFactory.LoadTagsToIgnoreCompletely(context);
        TagsTextToIgnore = TagsFactory.LoadTagsTextToIgnore(context);
    }
}
