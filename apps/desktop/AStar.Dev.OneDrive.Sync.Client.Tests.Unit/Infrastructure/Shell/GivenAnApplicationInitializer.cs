using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Shell;

public sealed class GivenAnApplicationInitializer
{
    private readonly IStartupService _startupService = Substitute.For<IStartupService>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncService _syncService = Substitute.For<ISyncService>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();

    public GivenAnApplicationInitializer()
    {
        _settingsService.Current.Returns(new AppSettings());
        _syncRepository.GetPendingConflictsAsync(Arg.Any<string>()).Returns([]);
    }

    private AccountsViewModel CreateAccountsViewModel() => new(_authService, _graphService, _accountRepository, _syncEventAggregator);
    private FilesViewModel CreateFilesViewModel() => new(_authService, _graphService, _accountRepository);
    private DashboardViewModel CreateDashboardViewModel() => new(_scheduler, _localizationService, _accountRepository, _syncEventAggregator);
    private ActivityViewModel CreateActivityViewModel() => new(_syncService, _syncRepository, _syncEventAggregator);
    private SettingsViewModel CreateSettingsViewModel() => new(_settingsService, _themeService, _scheduler, _accountRepository);

    private ApplicationInitializer CreateSut(AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings)
        => new(_startupService, accounts, files, dashboard, activity, settings);

    private static OneDriveAccount BuildAccount(string id = "acc-1", string email = "user@test.com", bool isActive = false)
        => new() { Id = id, DisplayName = "Test User", Email = email, IsActive = isActive, SelectedFolderIds = [] };

    [Fact]
    public async Task when_initialized_then_accounts_are_restored_from_startup_service()
    {
        _startupService.RestoreAccountsAsync().Returns([BuildAccount("acc-1"), BuildAccount("acc-2")]);

        var accounts = CreateAccountsViewModel();
        var sut = CreateSut(accounts, CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        accounts.Accounts.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_initialized_then_files_receives_all_restored_accounts()
    {
        _startupService.RestoreAccountsAsync().Returns([BuildAccount("acc-1"), BuildAccount("acc-2")]);

        var files = CreateFilesViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), files, CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        files.Tabs.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_initialized_then_dashboard_receives_all_restored_accounts()
    {
        _startupService.RestoreAccountsAsync().Returns([BuildAccount("acc-1"), BuildAccount("acc-2")]);

        var dashboard = CreateDashboardViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), dashboard, CreateActivityViewModel(), CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        dashboard.AccountSections.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_initialized_then_settings_loads_restored_accounts()
    {
        _startupService.RestoreAccountsAsync().Returns([BuildAccount("acc-1")]);

        var settings = CreateSettingsViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), settings);

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        settings.AccountSettings.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_active_account_exists_then_files_activates_that_account()
    {
        var active = BuildAccount("acc-active", isActive: true);
        _startupService.RestoreAccountsAsync().Returns([active, BuildAccount("acc-2")]);

        var files = CreateFilesViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), files, CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        files.ActiveTab.ShouldNotBeNull();
        files.ActiveTab!.AccountId.ShouldBe("acc-active");
    }

    [Fact]
    public async Task when_active_account_exists_then_activity_sets_active_account()
    {
        var active = BuildAccount("acc-active", email: "active@test.com", isActive: true);
        _startupService.RestoreAccountsAsync().Returns([active]);

        var activity = CreateActivityViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), CreateDashboardViewModel(), activity, CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).GetPendingConflictsAsync("acc-active");
    }

    [Fact]
    public async Task when_startup_service_throws_then_error_is_logged_and_not_rethrown()
    {
        _startupService.RestoreAccountsAsync().Returns(Task.FromException<List<OneDriveAccount>>(new InvalidOperationException("DB failure")));

        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel());

        var exception = await Record.ExceptionAsync(() => sut.InitializeAsync(TestContext.Current.CancellationToken));

        exception.ShouldBeNull();
    }
}
