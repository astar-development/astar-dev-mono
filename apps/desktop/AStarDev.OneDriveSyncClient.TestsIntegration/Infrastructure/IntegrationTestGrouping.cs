namespace AStarDev.OneDriveSyncClient.TestsIntegration.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestGrouping : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "Integration Tests";
}
