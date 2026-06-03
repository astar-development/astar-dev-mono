using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Theme;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAMainWindowViewModel
{
    private readonly IApplicationInitializer _initializer = Substitute.For<IApplicationInitializer>();
    private readonly ISyncScheduler _scheduler = Substitute.For<ISyncScheduler>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncEventAggregator _syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IThemeService _themeService = Substitute.For<IThemeService>();
    private readonly ILocalizationService _localizationService = Substitute.For<ILocalizationService>();
    private readonly ISyncService _syncService = Substitute.For<ISyncService>();
    private readonly ISyncRepository _syncRepository = Substitute.For<ISyncRepository>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public GivenAMainWindowViewModel()
    {
        _settingsService.Current.Returns(new AppSettings());
        _syncRepository.GetPendingConflictsAsync(Arg.Any<AccountId>()).Returns([]);
    }

    private AccountsViewModel CreateAccountsViewModel() => new(_authService, _graphService, _accountRepository, Substitute.For<IAccountOnboardingService>(), Substitute.For<IQuotaRefreshService>(), _syncEventAggregator, _localizationService, Substitute.For<ILogger<AccountsViewModel>>());
    private FilesViewModel CreateFilesViewModel() => new(_authService, _graphService, _accountRepository, Substitute.For<ISyncRuleRepository>(), _fileSystem, Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), Substitute.For<ILogger<FolderTreeNodeViewModel>>(), _localizationService);
    private DashboardViewModel CreateDashboardViewModel() => new(_scheduler, _localizationService, _accountRepository, _syncEventAggregator);
    private ActivityViewModel CreateActivityViewModel() => new(_syncService, _syncRepository, _syncEventAggregator, _localizationService, new InlineUiDispatcher());
    private static FileClassificationRulesViewModel CreateClassificationRulesViewModel()
    {
        var repo = Substitute.For<IFileClassificationRuleRepository>();
        repo.GetAllWithIdsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FileClassificationRuleEntry>>([]));

        return new FileClassificationRulesViewModel(repo);
    }

    private SettingsViewModel CreateSettingsViewModel() => new(_settingsService, _themeService, _scheduler, _accountRepository, _localizationService, Substitute.For<IFolderPickerService>());

    private MainWindowViewModel CreateSut()
    {
        var accountsVm = CreateAccountsViewModel();

        return new(_initializer, _scheduler, accountsVm, CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel(), CreateClassificationRulesViewModel(), new StatusBarViewModel(accountsVm, _localizationService), Substitute.For<ILogger<MainWindowViewModel>>());
    }

    [Fact]
    public void when_created_then_active_section_is_dashboard()
    {
        var sut = CreateSut();

        sut.ActiveSection.ShouldBe(NavSection.Dashboard);
    }

    [Fact]
    public void when_navigate_called_then_active_section_changes()
    {
        var sut = CreateSut();

        sut.NavigateCommand.Execute(NavSection.Files);

        sut.ActiveSection.ShouldBe(NavSection.Files);
    }

    [Fact]
    public void when_navigate_to_dashboard_then_is_dashboard_active_is_true()
    {
        var sut = CreateSut();
        sut.NavigateCommand.Execute(NavSection.Files);

        sut.NavigateCommand.Execute(NavSection.Dashboard);

        sut.IsDashboardActive.ShouldBeTrue();
    }

    [Fact]
    public void when_navigate_to_accounts_then_is_accounts_active_is_true()
    {
        var sut = CreateSut();

        sut.NavigateCommand.Execute(NavSection.Accounts);

        sut.IsAccountsActive.ShouldBeTrue();
    }

    [Fact]
    public void when_navigate_to_classifications_then_is_classifications_active_is_true()
    {
        var sut = CreateSut();

        sut.NavigateCommand.Execute(NavSection.Classifications);

        sut.IsClassificationsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task when_sync_now_command_executed_with_no_active_account_then_scheduler_not_called()
    {
        var sut = CreateSut();

        await sut.SyncNowCommand.ExecuteAsync(null);

        await _scheduler.DidNotReceive().TriggerAccountAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_sync_now_command_executed_with_active_account_then_scheduler_called_with_account_id()
    {
        const string accountIdStr = "active-account-123";
        var accountsVm = CreateAccountsViewModel();
        accountsVm.ActiveAccount = new AccountCardViewModel(new OneDriveAccount { Id = new AccountId(accountIdStr), Profile = AccountProfileFactory.Create("Test User", "test@example.com") }, _localizationService);
        var sut = new MainWindowViewModel(_initializer, _scheduler, accountsVm, CreateFilesViewModel(), CreateDashboardViewModel(), CreateActivityViewModel(), CreateSettingsViewModel(), CreateClassificationRulesViewModel(), new StatusBarViewModel(accountsVm, _localizationService), Substitute.For<ILogger<MainWindowViewModel>>());

        await sut.SyncNowCommand.ExecuteAsync(null);

        await _scheduler.Received(1).TriggerAccountAsync(accountIdStr, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_add_account_command_executed_then_navigates_to_accounts_section()
    {
        var sut = CreateSut();

        sut.AddAccountCommand.Execute(null);

        sut.ActiveSection.ShouldBe(NavSection.Accounts);
    }

    [Fact]
    public async Task when_initialise_async_called_then_delegates_to_application_initializer()
    {
        var sut = CreateSut();

        await sut.InitialiseAsync();

        await _initializer.Received(1).InitializeAsync(Arg.Any<CancellationToken>());
    }
}
