using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Dashboard;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Onboarding;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;
using AStar.Dev.OneDrive.Sync.Client.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Startup;

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
        _ = services.AddSingleton(Substitute.For<ISyncRepository>());
        _ = services.AddSingleton(Substitute.For<IAccountOnboardingService>());
        _ = services.AddSingleton(Substitute.For<IQuotaRefreshService>());
        _ = services.AddSingleton(Substitute.For<ISyncEventAggregator>());
        _ = services.AddSingleton(Substitute.For<ISyncService>());
        _ = services.AddSingleton(Substitute.For<ISyncScheduler>());
        _ = services.AddSingleton(Substitute.For<IUiDispatcher>());
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
