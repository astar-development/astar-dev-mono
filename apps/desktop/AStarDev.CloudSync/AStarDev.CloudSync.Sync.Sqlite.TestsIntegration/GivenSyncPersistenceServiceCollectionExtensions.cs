using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public class GivenSyncPersistenceServiceCollectionExtensions
{
    [Fact]
    public void when_the_module_is_added_then_the_same_collection_is_returned_for_chaining()
    {
        var services = new ServiceCollection();

        var returned = services.AddSyncPersistence();

        returned.ShouldBeSameAs(services);
    }
}
