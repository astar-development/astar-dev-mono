using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;
using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Search;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Shell;

public sealed class GivenAnApplicationInitializer
{
    private readonly IStartupService _startupService = Substitute.For<IStartupService>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly IQuotaRefreshService _quotaRefreshService = Substitute.For<IQuotaRefreshService>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncService _syncService = Substitute.For<ISyncService>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public GivenAnApplicationInitializer()
    {
        _settingsService.Current.Returns(new AppSettings());
        _syncRepository.GetPendingConflictsAsync(Arg.Any<AccountId>()).Returns([]);
    }

    private static Task<Result<List<OneDriveAccount>, string>> OkResult(List<OneDriveAccount> accounts)
        => Task.FromResult<Result<List<OneDriveAccount>, string>>(new Result<List<OneDriveAccount>, string>.Ok(accounts));

    private static Task<Result<List<OneDriveAccount>, string>> ErrorResult(string message)
        => Task.FromResult<Result<List<OneDriveAccount>, string>>(new Result<List<OneDriveAccount>, string>.Error(message));

    private AccountsViewModel CreateAccountsViewModel() => new(_authService, _graphService, _accountRepository, Substitute.For<IAccountOnboardingService>(), _quotaRefreshService, _syncEventAggregator, new AddAccountWizardViewModelFactory(_authService, _graphService, _localizationService), new AccountCardViewModelFactory(_localizationService), Substitute.For<ILogger<AccountsViewModel>>());
    private FilesViewModel CreateFilesViewModel() => new(new AccountFilesViewModelFactory(_authService, _graphService, Substitute.For<ISyncRuleService>(), _fileSystem, Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), new FolderTreeNodeViewModelFactory(_graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), _localizationService), _localizationService), _localizationService);
    private DashboardViewModel CreateDashboardViewModel() => new(_localizationService, _syncEventAggregator, new DashboardAccountViewModelFactory(_scheduler, _accountRepository, _localizationService, new ActivityItemViewModelFactory(_localizationService)), new ActivityItemViewModelFactory(_localizationService), new ManualUiTimer());
    private ActivityViewModel CreateActivityViewModel() => new(_syncRepository, _syncEventAggregator, new ConflictItemViewModelFactory(_syncService, _localizationService), new ActivityItemViewModelFactory(_localizationService), new InlineUiDispatcher(), _localizationService);
    private SettingsViewModel CreateSettingsViewModel() => new(_settingsService, _themeService, _scheduler, _accountRepository, _localizationService, Substitute.For<IFolderPickerService>());

    private SyncedFileSearchViewModel CreateSearchViewModel() => new(Substitute.For<ISyncedItemRepository>(), Substitute.For<IFileOpenerService>(), Substitute.For<IFileTypeClassifier>(), _accountRepository, new InlineUiDispatcher(), _localizationService);

    private ApplicationInitializer CreateSut(AccountsViewModel accounts, FilesViewModel files, DashboardViewModel dashboard, ActivityViewModel activity, SettingsViewModel settings)
        => new(_startupService, _quotaRefreshService, accounts, files, dashboard, activity, settings, CreateSearchViewModel(), Substitute.For<ILogger<ApplicationInitializer>>());

    private static OneDriveAccount BuildAccount(string id = "acc-1", string email = "user@test.com", bool isActive = false)
        => new() { Id = new AccountId(id), Profile = AccountProfileFactory.Create("Test User", email), IsActive = isActive, SelectedFolderIds = [] };

    [Fact]
    public async Task when_initialized_then_accounts_are_restored_from_startup_service()
    {
        _startupService.RestoreAccountsAsync().Returns(OkResult([BuildAccount("acc-1"), BuildAccount("acc-2")]));

        var accounts = CreateAccountsViewModel();
        var sut = CreateSut(accounts, CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        accounts.Accounts.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_initialized_then_files_receives_all_restored_accounts()
    {
        _startupService.RestoreAccountsAsync().Returns(OkResult([BuildAccount("acc-1"), BuildAccount("acc-2")]));

        var files = CreateFilesViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), files, CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        files.Tabs.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_initialized_then_dashboard_receives_all_restored_accounts()
    {
        _startupService.RestoreAccountsAsync().Returns(OkResult([BuildAccount("acc-1"), BuildAccount("acc-2")]));

        var dashboard = CreateDashboardViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), dashboard, CreateActivityViewModel(), CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        dashboard.AccountSections.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_initialized_then_settings_loads_restored_accounts()
    {
        _startupService.RestoreAccountsAsync().Returns(OkResult([BuildAccount("acc-1")]));

        var settings = CreateSettingsViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), settings);

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        settings.AccountSettings.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_initialized_then_quota_refresh_is_called_for_each_restored_account()
    {
        var acc1 = BuildAccount("acc-1");
        var acc2 = BuildAccount("acc-2");
        _startupService.RestoreAccountsAsync().Returns(OkResult([acc1, acc2]));

        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        await _quotaRefreshService.Received(1).TryRefreshAsync(acc1, Arg.Any<CancellationToken>());
        await _quotaRefreshService.Received(1).TryRefreshAsync(acc2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_active_account_exists_then_files_activates_that_account()
    {
        var active = BuildAccount("acc-active", isActive: true);
        _startupService.RestoreAccountsAsync().Returns(OkResult([active, BuildAccount("acc-2")]));

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
        _startupService.RestoreAccountsAsync().Returns(OkResult([active]));

        var activity = CreateActivityViewModel();
        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), CreateDashboardViewModel(), activity, CreateSettingsViewModel());

        await sut.InitializeAsync(TestContext.Current.CancellationToken);

        await _syncRepository.Received(1).GetPendingConflictsAsync(new AccountId("acc-active"), CancellationToken.None);
    }

    [Fact]
    public async Task when_startup_service_returns_error_then_exception_is_thrown_and_rethrown()
    {
        _startupService.RestoreAccountsAsync().Returns(ErrorResult("DB failure"));

        var sut = CreateSut(CreateAccountsViewModel(), CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel());

        var exception = await Record.ExceptionAsync(() => sut.InitializeAsync(TestContext.Current.CancellationToken));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<InvalidOperationException>();
        exception!.Message.ShouldBe("DB failure");
    }
}
