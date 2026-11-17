namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SharedConnectionPool
{
    public const string Name = "Shared connection pool";
}
