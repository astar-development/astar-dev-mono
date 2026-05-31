using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data;
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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Shell;

public sealed class GivenAnAppBootstrapper : IAsyncDisposable
{
    private readonly ISettingsService settingsService = Substitute.For<ISettingsService>();
    private readonly IThemeService themeService = Substitute.For<IThemeService>();
    private readonly ISyncScheduler syncScheduler = Substitute.For<ISyncScheduler>();
    private readonly IApplicationInitializer applicationInitializer = Substitute.For<IApplicationInitializer>();
    private readonly IAuthService authService = Substitute.For<IAuthService>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();
    private readonly ISyncRuleRepository syncRuleRepository = Substitute.For<ISyncRuleRepository>();
    private readonly ISyncEventAggregator syncEventAggregator = Substitute.For<ISyncEventAggregator>();
    private readonly ISyncService syncService = Substitute.For<ISyncService>();
    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly ILocalizationService localizationService = Substitute.For<ILocalizationService>();
    private readonly IStartupService startupService = Substitute.For<IStartupService>();
    private readonly ISettingsService settingsServiceForViewModel = Substitute.For<ISettingsService>();
    private readonly IThemeService themeServiceForViewModel = Substitute.For<IThemeService>();
    private readonly ISyncScheduler schedulerForViewModel = Substitute.For<ISyncScheduler>();
    private readonly IFileSystem fileSystem = Substitute.For<IFileSystem>();
    private readonly SqliteConnection sqliteConnection;
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public GivenAnAppBootstrapper()
    {
        settingsService.Current.Returns(new AppSettings { SyncIntervalMinutes = 30, Theme = AppTheme.System });
        settingsServiceForViewModel.Current.Returns(new AppSettings());
        startupService.RestoreAccountsAsync().Returns([]);
        syncRepository.GetPendingConflictsAsync(Arg.Any<AccountId>()).Returns([]);

        sqliteConnection = new SqliteConnection("Data Source=:memory:");
        sqliteConnection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(sqliteConnection)
            .Options;

        dbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
    }

    public async ValueTask DisposeAsync() => await sqliteConnection.DisposeAsync();

    private MainWindowViewModel CreateMainWindowViewModel()
    {
        var accounts = new AccountsViewModel(authService, graphService, accountRepository, Substitute.For<IAccountOnboardingService>(), syncEventAggregator, localizationService, Substitute.For<ILogger<AccountsViewModel>>());
        var files = new FilesViewModel(authService, graphService, accountRepository, syncRuleRepository, fileSystem, Substitute.For<IFileManagerService>(), Substitute.For<ILogger<AccountFilesViewModel>>(), Substitute.For<ILogger<FolderTreeNodeViewModel>>(), localizationService);
        var dashboard = new DashboardViewModel(schedulerForViewModel, localizationService, accountRepository, syncEventAggregator);
        var activity = new ActivityViewModel(syncService, syncRepository, syncEventAggregator, localizationService);
        var classificationRulesRepo = Substitute.For<IFileClassificationRuleRepository>();
        classificationRulesRepo.GetAllWithIdsAsync(Arg.Any<CancellationToken>())
                               .Returns(Task.FromResult<IReadOnlyList<FileClassificationRuleEntry>>([]));
        var settings = new SettingsViewModel(settingsServiceForViewModel, themeServiceForViewModel, schedulerForViewModel, accountRepository, localizationService);
        var statusBar = new StatusBarViewModel(accounts);

        return new MainWindowViewModel(applicationInitializer, syncScheduler, accounts, files, dashboard, activity, settings, new FileClassificationRulesViewModel(classificationRulesRepo), statusBar, Substitute.For<ILogger<MainWindowViewModel>>());
    }

    private AppBootstrapper CreateSut() => new(dbContextFactory, settingsService, themeService, syncScheduler, CreateMainWindowViewModel(), Substitute.For<ILogger<AppBootstrapper>>());

    [Fact]
    public async Task when_bootstrap_async_is_called_then_settings_load_async_is_called()
    {
        var sut = CreateSut();

        await sut.BootstrapAsync(new Progress<string>(), TestContext.Current.CancellationToken);

        await settingsService.Received(1).LoadAsync();
    }

    [Fact]
    public async Task when_bootstrap_async_is_called_then_theme_service_apply_is_called()
    {
        var sut = CreateSut();

        await sut.BootstrapAsync(new Progress<string>(), TestContext.Current.CancellationToken);

        themeService.Received(1).Apply(Arg.Any<AppTheme>());
    }

    [Fact]
    public async Task when_bootstrap_async_is_called_then_sync_scheduler_start_sync_is_called()
    {
        var sut = CreateSut();

        await sut.BootstrapAsync(new Progress<string>(), TestContext.Current.CancellationToken);

        syncScheduler.Received(1).StartSync(Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task when_bootstrap_async_is_called_then_startup_calls_are_made_in_correct_order()
    {
        var callOrder = new List<string>();
        settingsService.LoadAsync().Returns(_ =>
        {
            callOrder.Add("LoadAsync");
            return Task.CompletedTask;
        });
        themeService.When(service => service.Apply(Arg.Any<AppTheme>())).Do(_ => callOrder.Add("Apply"));
        syncScheduler.When(scheduler => scheduler.StartSync(Arg.Any<TimeSpan?>())).Do(_ => callOrder.Add("StartSync"));

        var sut = CreateSut();

        await sut.BootstrapAsync(new Progress<string>(), TestContext.Current.CancellationToken);

        callOrder.ShouldBe(["LoadAsync", "Apply", "StartSync"]);
    }

    [Fact]
    public async Task when_bootstrap_async_is_called_then_progress_messages_are_reported()
    {
        var reported = new List<string>();
        var progress = Substitute.For<IProgress<string>>();
        progress.When(p => p.Report(Arg.Any<string>())).Do(call => reported.Add(call.Arg<string>()));
        var sut = CreateSut();

        await sut.BootstrapAsync(progress, TestContext.Current.CancellationToken);

        reported.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task when_bootstrap_async_is_called_then_theme_is_applied_with_current_settings_theme()
    {
        settingsService.Current.Returns(new AppSettings { Theme = AppTheme.Dark, SyncIntervalMinutes = 60 });
        var sut = CreateSut();

        await sut.BootstrapAsync(new Progress<string>(), TestContext.Current.CancellationToken);

        themeService.Received(1).Apply(AppTheme.Dark);
    }

    [Fact]
    public async Task when_bootstrap_async_is_called_then_scheduler_starts_with_interval_from_settings()
    {
        settingsService.Current.Returns(new AppSettings { SyncIntervalMinutes = 15, Theme = AppTheme.System });
        var sut = CreateSut();

        await sut.BootstrapAsync(new Progress<string>(), TestContext.Current.CancellationToken);

        syncScheduler.Received(1).StartSync(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task when_bootstrap_async_throws_then_exception_is_rethrown()
    {
        settingsService.LoadAsync().Returns(Task.FromException(new InvalidOperationException("Settings failure")));
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.BootstrapAsync(new Progress<string>(), TestContext.Current.CancellationToken));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<InvalidOperationException>();
        exception!.Message.ShouldBe("Settings failure");
    }
}
