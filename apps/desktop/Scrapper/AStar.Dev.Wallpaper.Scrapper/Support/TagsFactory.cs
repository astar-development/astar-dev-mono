using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Support;

public static class TagsFactory
{
    public static TagsToIgnoreCompletely LoadTagsToIgnoreCompletely(FilesContext dbContext)
        => new() { Tags = [.. dbContext.TagsToIgnore.Where(t => t.IgnoreImage).Select(t => t.Value)] };

    public static TagsTextToIgnore LoadTagsTextToIgnore(FilesContext dbContext)
        => new() { Tags = [.. dbContext.TagsToIgnore.Where(t => !t.IgnoreImage).Select(t => t.Value)] };
}
