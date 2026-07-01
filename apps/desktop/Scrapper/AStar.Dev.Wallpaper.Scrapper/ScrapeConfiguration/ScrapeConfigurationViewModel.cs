using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.Infrastructure.FilesDb.Data;
using AStar.Dev.Infrastructure.FilesDb.Models;
using AStar.Dev.Wallpaper.Scrapper.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scrapper.ScrapeConfigurationEditor;

public class ScrapeConfigurationViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly FilesContext _context;
    private ScrapeConfigurationEntity? _entity;

    private bool _isLoading;
    private string _statusMessage = string.Empty;

    private string _sqlite = string.Empty;

    private string _loginEmailAddress = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _sessionCookie = string.Empty;

    private string _baseUrl = string.Empty;
    private string _apiKey = string.Empty;
    private string _loginUrl = string.Empty;
    private string _searchString = string.Empty;
    private string _searchStringPrefix = string.Empty;
    private string _searchStringSuffix = string.Empty;
    private string _topWallpapers = string.Empty;
    private string _subscriptions = string.Empty;
    private int _imagePauseInSeconds;
    private int _startingPageNumber;
    private int _totalPages;
    private bool _useHeadless;
    private decimal? _slowMotionDelay;
    private int _subscriptionsStartingPageNumber;
    private int _subscriptionsTotalPages;
    private int _topWallpapersTotalPages;
    private int _topWallpapersStartingPageNumber;

    private string _rootDirectory = string.Empty;
    private string _baseSaveDirectory = string.Empty;
    private string _baseDirectory = string.Empty;
    private string _baseDirectoryFamous = string.Empty;
    private string _subDirectoryName = string.Empty;

    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

    internal void UpdateStatus(string message) => StatusMessage = message;

    public string Sqlite { get => _sqlite; set => SetProperty(ref _sqlite, value); }

    public string LoginEmailAddress { get => _loginEmailAddress; set => SetProperty(ref _loginEmailAddress, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string SessionCookie { get => _sessionCookie; set => SetProperty(ref _sessionCookie, value); }

    public string BaseUrl { get => _baseUrl; set => SetProperty(ref _baseUrl, value); }
    public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
    public string LoginUrl { get => _loginUrl; set => SetProperty(ref _loginUrl, value); }
    public string SearchString { get => _searchString; set => SetProperty(ref _searchString, value); }
    public string SearchStringPrefix { get => _searchStringPrefix; set => SetProperty(ref _searchStringPrefix, value); }
    public string SearchStringSuffix { get => _searchStringSuffix; set => SetProperty(ref _searchStringSuffix, value); }
    public string TopWallpapers { get => _topWallpapers; set => SetProperty(ref _topWallpapers, value); }
    public string Subscriptions { get => _subscriptions; set => SetProperty(ref _subscriptions, value); }
    public int ImagePauseInSeconds { get => _imagePauseInSeconds; set => SetProperty(ref _imagePauseInSeconds, value); }
    public int StartingPageNumber { get => _startingPageNumber; set => SetProperty(ref _startingPageNumber, value); }
    public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }
    public bool UseHeadless { get => _useHeadless; set => SetProperty(ref _useHeadless, value); }
    public decimal? SlowMotionDelay { get => _slowMotionDelay; set => SetProperty(ref _slowMotionDelay, value); }
    public int SubscriptionsStartingPageNumber { get => _subscriptionsStartingPageNumber; set => SetProperty(ref _subscriptionsStartingPageNumber, value); }
    public int SubscriptionsTotalPages { get => _subscriptionsTotalPages; set => SetProperty(ref _subscriptionsTotalPages, value); }
    public int TopWallpapersTotalPages { get => _topWallpapersTotalPages; set => SetProperty(ref _topWallpapersTotalPages, value); }
    public int TopWallpapersStartingPageNumber { get => _topWallpapersStartingPageNumber; set => SetProperty(ref _topWallpapersStartingPageNumber, value); }

    public string RootDirectory { get => _rootDirectory; set => SetProperty(ref _rootDirectory, value); }
    public string BaseSaveDirectory { get => _baseSaveDirectory; set => SetProperty(ref _baseSaveDirectory, value); }
    public string BaseDirectory { get => _baseDirectory; set => SetProperty(ref _baseDirectory, value); }
    public string BaseDirectoryFamous { get => _baseDirectoryFamous; set => SetProperty(ref _baseDirectoryFamous, value); }
    public string SubDirectoryName { get => _subDirectoryName; set => SetProperty(ref _subDirectoryName, value); }

    public ObservableCollection<SearchCategoryViewModel> SearchCategories { get; } = [];

    public ICommand SaveCommand { get; }

    public ScrapeConfigurationViewModel(IDbContextFactory<FilesContext> contextFactory)
    {
        _context = contextFactory.CreateDbContext();
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            _entity = await _context.ScrapeConfiguration
                .Include(e => e.ConnectionStrings)
                .Include(e => e.UserConfiguration)
                .Include(e => e.SearchConfiguration).ThenInclude(sc => sc.SearchCategories)
                .Include(e => e.ScrapeDirectories).OrderByDescending(s => s.Id)
                .FirstAsync();

            MapFromEntity(_entity);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private void MapFromEntity(ScrapeConfigurationEntity entity)
    {
        Sqlite = entity.ConnectionStrings.Sqlite;

        LoginEmailAddress = entity.UserConfiguration.LoginEmailAddress;
        Username = entity.UserConfiguration.Username;
        Password = entity.UserConfiguration.Password;
        SessionCookie = entity.UserConfiguration.SessionCookie;

        var search = entity.SearchConfiguration;
        BaseUrl = search.BaseUrl.ToString();
        ApiKey = search.ApiKey;
        LoginUrl = search.LoginUrl.ToString();
        SearchString = search.SearchString;
        SearchStringPrefix = search.SearchStringPrefix;
        SearchStringSuffix = search.SearchStringSuffix;
        TopWallpapers = search.TopWallpapers;
        Subscriptions = search.Subscriptions;
        ImagePauseInSeconds = search.ImagePauseInSeconds;
        StartingPageNumber = search.StartingPageNumber;
        TotalPages = search.TotalPages;
        UseHeadless = search.UseHeadless;
        SlowMotionDelay = (decimal?)search.SlowMotionDelay;
        SubscriptionsStartingPageNumber = search.SubscriptionsStartingPageNumber;
        SubscriptionsTotalPages = search.SubscriptionsTotalPages;
        TopWallpapersTotalPages = search.TopWallpapersTotalPages;
        TopWallpapersStartingPageNumber = search.TopWallpapersStartingPageNumber;

        var dirs = entity.ScrapeDirectories;
        RootDirectory = dirs.RootDirectory;
        BaseSaveDirectory = dirs.BaseSaveDirectory;
        BaseDirectory = dirs.BaseDirectory;
        BaseDirectoryFamous = dirs.BaseDirectoryFamous;
        SubDirectoryName = dirs.SubDirectoryName;

        SearchCategories.Clear();
        foreach (var category in search.SearchCategories)
            SearchCategories.Add(SearchCategoryViewModel.FromEntity(category));
    }

    private async Task SaveAsync()
    {
        if (_entity is null)
            return;

        StatusMessage = string.Empty;

        try
        {
            MapToEntity(_entity);
            await _context.SaveChangesAsync();
            StatusMessage = "Saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    private void MapToEntity(ScrapeConfigurationEntity entity)
    {
        entity.ConnectionStrings.Sqlite = Sqlite;

        entity.UserConfiguration.LoginEmailAddress = LoginEmailAddress;
        entity.UserConfiguration.Username = Username;
        entity.UserConfiguration.Password = Password;
        entity.UserConfiguration.SessionCookie = SessionCookie;

        var search = entity.SearchConfiguration;
        search.BaseUrl = new Uri(BaseUrl);
        search.ApiKey = ApiKey;
        search.LoginUrl = new Uri(LoginUrl);
        search.SearchString = SearchString;
        search.SearchStringPrefix = SearchStringPrefix;
        search.SearchStringSuffix = SearchStringSuffix;
        search.TopWallpapers = TopWallpapers;
        search.Subscriptions = Subscriptions;
        search.ImagePauseInSeconds = ImagePauseInSeconds;
        search.StartingPageNumber = StartingPageNumber;
        search.TotalPages = TotalPages;
        search.UseHeadless = UseHeadless;
        search.SlowMotionDelay = (float?)SlowMotionDelay;
        search.SubscriptionsStartingPageNumber = SubscriptionsStartingPageNumber;
        search.SubscriptionsTotalPages = SubscriptionsTotalPages;
        search.TopWallpapersTotalPages = TopWallpapersTotalPages;
        search.TopWallpapersStartingPageNumber = TopWallpapersStartingPageNumber;

        foreach (var categoryVm in SearchCategories)
        {
            var existing = search.SearchCategories.FirstOrDefault(c => c.Id == categoryVm.Id);
            if (existing is not null)
                categoryVm.ApplyTo(existing);
        }

        var dirs = entity.ScrapeDirectories;
        dirs.RootDirectory = RootDirectory;
        dirs.BaseSaveDirectory = BaseSaveDirectory;
        dirs.BaseDirectory = BaseDirectory;
        dirs.BaseDirectoryFamous = BaseDirectoryFamous;
        dirs.SubDirectoryName = SubDirectoryName;
    }
}
