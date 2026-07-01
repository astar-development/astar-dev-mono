using AStar.Dev.Wallpaper.Scrapper.ViewModels;
using DbSearchCategories = AStar.Dev.Infrastructure.FilesDb.Models.SearchCategories;

namespace AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;

public class SearchCategoryViewModel : ViewModelBase
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private int _lastKnownImageCount;
    private int _lastPageVisited;
    private int _totalPages;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public int LastKnownImageCount { get => _lastKnownImageCount; set => SetProperty(ref _lastKnownImageCount, value); }
    public int LastPageVisited { get => _lastPageVisited; set => SetProperty(ref _lastPageVisited, value); }
    public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }

    internal void ApplyTo(DbSearchCategories entity)
    {
        entity.Id = Id;
        entity.Name = Name;
        entity.LastKnownImageCount = LastKnownImageCount;
        entity.LastPageVisited = LastPageVisited;
        entity.TotalPages = TotalPages;
    }

    internal static SearchCategoryViewModel FromEntity(DbSearchCategories entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        LastKnownImageCount = entity.LastKnownImageCount,
        LastPageVisited = entity.LastPageVisited,
        TotalPages = entity.TotalPages
    };
}
