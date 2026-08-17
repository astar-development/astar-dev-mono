using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.OneDriveSyncClient.TestsIntegration.Infrastructure;

[Collection(IntegrationTestGrouping.Name)]
public sealed class GivenTheIntegrationTestFixture(IntegrationTestFixture fixture)
{
    [Fact]
    public void when_resolving_services_then_sync_service_is_not_null()
        => fixture.Services.GetRequiredService<ISyncService>().ShouldNotBeNull();

    [Fact]
    public void when_resolving_services_then_synced_item_registrar_is_not_null()
        => fixture.Services.GetRequiredService<ISyncedItemRegistrar>().ShouldNotBeNull();

    [Fact]
    public void when_resolving_services_then_file_auto_categorisor_is_not_null()
        => fixture.Services.GetRequiredService<IFileAutoCategorisor>().ShouldNotBeNull();
}
