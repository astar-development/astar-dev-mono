namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestGrouping : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration Tests";
}
