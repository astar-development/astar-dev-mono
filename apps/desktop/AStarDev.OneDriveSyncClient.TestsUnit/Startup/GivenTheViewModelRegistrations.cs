using AStarDev.LoggingSerilog.LogViewer;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Dashboard;
using AStarDev.OneDriveSyncClient.Data;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Onboarding;
using AStarDev.OneDriveSyncClient.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Startup;

public sealed class GivenTheViewModelRegistrations
{
    private static ServiceProvider ServiceProvider = null!;
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var inMemoryLogSink = new InMemoryLogSink();
        services.AddViewModels().AddShell(inMemoryLogSink).AddLocalizationServices().AddPersistence().AddLogging();
        App.RegisterOptions(services);
        ServiceProvider = services.BuildServiceProvider();
        return ServiceProvider;
    }

    [Fact]
    public async Task when_the_container_is_built_then_every_view_model_factory_resolves()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<IAccountCardViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IAccountFilesViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IActivityItemViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IAddAccountWizardViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IConflictItemViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IDashboardAccountViewModelFactory>().ShouldNotBeNull();
        provider.GetRequiredService<IFolderTreeNodeViewModelFactory>().ShouldNotBeNull();
        if (provider is not null)
            await provider.DisposeAsync();
    }

    [Fact]
    public async Task when_the_container_is_built_then_the_factory_consuming_view_models_resolve()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<AccountsViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<ActivityViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<DashboardViewModel>().ShouldNotBeNull();
        provider.GetRequiredService<FilesViewModel>().ShouldNotBeNull();
        if (provider is not null)
            await provider.DisposeAsync();
    }
}
