using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Authentication;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Onboarding;
using AStarDev.OneDriveSyncClient.Infrastructure.Rules;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Onboarding;
using AStarDev.OneDriveSyncClient.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Startup;

public sealed class GivenTheViewModelRegistrations
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        _ = services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        _ = services.AddSingleton(Substitute.For<IAuthService>());
        _ = services.AddSingleton(Substitute.For<IGraphService>());
        _ = services.AddSingleton(Substitute.For<IAccountRepository>());
        _ = services.AddSingleton(Substitute.For<ISyncRuleRepository>());
        _ = services.AddSingleton(Substitute.For<ISyncRuleService>());
        _ = services.AddSingleton(Substitute.For<ISyncRepository>());
        _ = services.AddSingleton(Substitute.For<IAccountOnboardingService>());
        _ = services.AddSingleton(Substitute.For<IQuotaRefreshService>());
        _ = services.AddSingleton(Substitute.For<ISyncEventAggregator>());
        _ = services.AddSingleton(Substitute.For<ISyncService>());
        _ = services.AddSingleton(Substitute.For<ISyncScheduler>());
        _ = services.AddSingleton(Substitute.For<IUiDispatcher>());
        _ = services.AddSingleton(Substitute.For<IUiTimer>());
        _ = services.AddSingleton(Substitute.For<ILocalizationService>());
        _ = services.AddSingleton(Substitute.For<IFileSystem>());
        _ = services.AddSingleton(Substitute.For<IFileManagerService>());

        return services.AddViewModels().BuildServiceProvider();
    }

    [Fact]
    public void when_the_container_is_built_then_every_view_model_factory_resolves()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<IAccountCardViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IAccountFilesViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IActivityItemViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IAddAccountWizardViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IConflictItemViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IDashboardAccountViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IFolderTreeNodeViewModelFactory>().ShouldNotBeNull();
    }

    [Fact]
    public void when_the_container_is_built_then_the_factory_consuming_view_models_resolve()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<AccountsViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<ActivityViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<DashboardViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<FilesViewModel>().ShouldNotBeNull();
    }
}
