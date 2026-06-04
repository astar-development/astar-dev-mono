using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;

public sealed class GivenTheIntegrationTestFixture(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
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

    [Fact]
    public void when_resolving_services_then_file_classification_rule_repository_is_not_null()
        => fixture.Services.GetRequiredService<IFileClassificationRuleRepository>().ShouldNotBeNull();
}
