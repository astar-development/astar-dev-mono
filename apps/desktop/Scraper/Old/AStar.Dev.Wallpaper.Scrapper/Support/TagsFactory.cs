using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public static class TagsFactory
{
    public static TagsToIgnoreCompletely LoadTagsToIgnoreCompletely(AppDbContext dbContext)
        => new() { Tags = [.. dbContext.TagsToIgnore.Where(t => t.IgnoreImage).Select(t => t.Value)] };

    public static TagsTextToIgnore LoadTagsTextToIgnore(AppDbContext dbContext)
        => new() { Tags = [.. dbContext.TagsToIgnore.Where(t => !t.IgnoreImage).Select(t => t.Value)] };
}
