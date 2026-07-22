using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Models;
using AStar.Dev.Wallpaper.Scraper.Support;
using AStar.Dev.Wallpaper.Scraper.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.ScrapeConfigurationEditor;

public class ScrapeConfigurationViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> contextFactory;
    private readonly ScrapeConfigurationManager scrapeConfigurationManager;
    private ScrapeConfigurationEntity? entity;

    private bool isLoading;
    private string statusMessage = string.Empty;

    private string sqlite = string.Empty;

    private string loginEmailAddress = string.Empty;
    private string username = string.Empty;
    private string password = string.Empty;
    private string sessionCookie = string.Empty;

    private string baseUrl = string.Empty;
    private string apiKey = string.Empty;
    private string loginUrl = string.Empty;
    private string searchString = string.Empty;
    private string searchStringPrefix = string.Empty;
    private string searchStringSuffix = string.Empty;
    private string topWallpapers = string.Empty;
    private string subscriptions = string.Empty;
    private int imagePauseInSeconds;
    private int startingPageNumber;
    private int totalPages;
    private bool useHeadless;
    private decimal? slowMotionDelay;
    private int subscriptionsStartingPageNumber;
    private int subscriptionsTotalPages;
    private int topWallpapersTotalPages;
    private int topWallpapersStartingPageNumber;

    private string rootDirectory = string.Empty;
    private string baseSaveDirectory = string.Empty;
    private string baseDirectory = string.Empty;
    private string baseDirectoryFamous = string.Empty;
    private string subDirectoryName = string.Empty;

    public bool IsLoading { get => isLoading; private set => SetProperty(ref isLoading, value); }
    public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }

    internal void UpdateStatus(string message) => StatusMessage = message;

    public string Sqlite { get => sqlite; set => SetProperty(ref sqlite, value); }

    public string LoginEmailAddress { get => loginEmailAddress; set => SetProperty(ref loginEmailAddress, value); }
    public string Username { get => username; set => SetProperty(ref username, value); }
    public string Password { get => password; set => SetProperty(ref password, value); }
    public string SessionCookie { get => sessionCookie; set => SetProperty(ref sessionCookie, value); }

    public string BaseUrl { get => baseUrl; set => SetProperty(ref baseUrl, value); }
    public string ApiKey { get => apiKey; set => SetProperty(ref apiKey, value); }
    public string LoginUrl { get => loginUrl; set => SetProperty(ref loginUrl, value); }
    public string SearchString { get => searchString; set => SetProperty(ref searchString, value); }
    public string SearchStringPrefix { get => searchStringPrefix; set => SetProperty(ref searchStringPrefix, value); }
    public string SearchStringSuffix { get => searchStringSuffix; set => SetProperty(ref searchStringSuffix, value); }
    public string TopWallpapers { get => topWallpapers; set => SetProperty(ref topWallpapers, value); }
    public string Subscriptions { get => subscriptions; set => SetProperty(ref subscriptions, value); }
    public int ImagePauseInSeconds { get => imagePauseInSeconds; set => SetProperty(ref imagePauseInSeconds, value); }
    public int StartingPageNumber { get => startingPageNumber; set => SetProperty(ref startingPageNumber, value); }
    public int TotalPages { get => totalPages; set => SetProperty(ref totalPages, value); }
    public bool UseHeadless { get => useHeadless; set => SetProperty(ref useHeadless, value); }
    public decimal? SlowMotionDelay { get => slowMotionDelay; set => SetProperty(ref slowMotionDelay, value); }
    public int SubscriptionsStartingPageNumber { get => subscriptionsStartingPageNumber; set => SetProperty(ref subscriptionsStartingPageNumber, value); }
    public int SubscriptionsTotalPages { get => subscriptionsTotalPages; set => SetProperty(ref subscriptionsTotalPages, value); }
    public int TopWallpapersTotalPages { get => topWallpapersTotalPages; set => SetProperty(ref topWallpapersTotalPages, value); }
    public int TopWallpapersStartingPageNumber { get => topWallpapersStartingPageNumber; set => SetProperty(ref topWallpapersStartingPageNumber, value); }

    public string RootDirectory { get => rootDirectory; set => SetProperty(ref rootDirectory, value); }
    public string BaseSaveDirectory { get => baseSaveDirectory; set => SetProperty(ref baseSaveDirectory, value); }
    public string BaseDirectory { get => baseDirectory; set => SetProperty(ref baseDirectory, value); }
    public string BaseDirectoryFamous { get => baseDirectoryFamous; set => SetProperty(ref baseDirectoryFamous, value); }
    public string SubDirectoryName { get => subDirectoryName; set => SetProperty(ref subDirectoryName, value); }

    public ObservableCollection<SearchCategoryViewModel> SearchCategories { get; } = [];

    public ICommand SaveCommand { get; }

    public ScrapeConfigurationViewModel(IDbContextFactory<AppDbContext> contextFactory, ScrapeConfigurationManager scrapeConfigurationManager)
    {
        this.contextFactory = contextFactory;
        this.scrapeConfigurationManager = scrapeConfigurationManager;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public async Task<Result<FunctionalParadigm.Unit, ScrapeError>> LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            entity = await context.ScrapeConfiguration
                .Include(e => e.ConnectionStrings)
                .Include(e => e.UserConfiguration)
                .Include(e => e.SearchConfiguration).ThenInclude(sc => sc.SearchCategories)
                .Include(e => e.ScrapeDirectories).OrderByDescending(s => s.Id)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            MapFromEntity(entity);

            return FunctionalParadigm.Unit.Instance;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load: {ex.Message}";

            return new RepositoryOperationFailed(nameof(LoadAsync), ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<Result<FunctionalParadigm.Unit, ScrapeError>> SaveAsync(CancellationToken cancellationToken)
    {
        if (entity is null)
            return new RepositoryOperationFailed(nameof(SaveAsync), "No configuration loaded");

        StatusMessage = string.Empty;

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var tracked = await context.ScrapeConfiguration
                .Include(e => e.ConnectionStrings)
                .Include(e => e.UserConfiguration)
                .Include(e => e.SearchConfiguration).ThenInclude(sc => sc.SearchCategories)
                .Include(e => e.ScrapeDirectories)
                .FirstAsync(e => e.Id == entity.Id, cancellationToken)
                .ConfigureAwait(false);

            MapToEntity(tracked);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scrapeConfigurationManager.ReloadAsync(cancellationToken).ConfigureAwait(false);

            StatusMessage = "Saved successfully.";

            return FunctionalParadigm.Unit.Instance;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";

            return new RepositoryOperationFailed(nameof(SaveAsync), ex.Message);
        }
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
