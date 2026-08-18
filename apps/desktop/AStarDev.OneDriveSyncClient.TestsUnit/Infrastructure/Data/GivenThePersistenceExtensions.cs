using AStar.Dev.Infrastructure.AppDb;
using AStarDev.OneDriveSyncClient.Data;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Data;

public sealed class GivenThePersistenceExtensions
{
    [Fact]
    public void when_add_persistence_is_called_then_all_expected_services_are_registered()
    {
        var services = new ServiceCollection().AddLogging();

        _ = services.AddPersistence();

        var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = false });

        serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<IAccountRepository>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<ISyncRepository>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<IDriveStateRepository>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<ISyncRuleRepository>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<ISyncedItemRepository>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<IFileClassificationRepository>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<IFileDetailResolver>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<ICategoryResolutionService>().ShouldNotBeNull();
        serviceProvider.GetRequiredService<ISyncPassRepositories>().ShouldNotBeNull();
    }
}
